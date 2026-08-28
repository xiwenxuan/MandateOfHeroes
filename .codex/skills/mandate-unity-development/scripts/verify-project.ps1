[CmdletBinding()]
param(
    [string]$ProjectPath,
    [ValidateRange(30, 300)]
    [int]$TimeoutSeconds = 300,
    [ValidateSet("EditMode", "PlayMode")]
    [string]$UnityTestPlatform = "EditMode",
    [string]$CoreTestFilter = "",
    [string]$UnityTestFilter = "",
    [switch]$SkipUnity,
    [switch]$DocumentationOnly
)

$ErrorActionPreference = "Stop"

function Get-NormalizedProcessEnvironment {
    # Codex and some CI hosts can place both `Path` and `PATH` in one Windows
    # process. Win32 treats them as the same variable, but MSBuild/Roslyn builds
    # a case-insensitive dictionary and fails with MSB3883 when both survive.
    # Build a clean environment block only for child processes; never modify
    # the current, user- or machine-level environment.
    $processEnvironment = [Environment]::GetEnvironmentVariables(
        [EnvironmentVariableTarget]::Process)
    $pathEntries = @(
        foreach ($key in $processEnvironment.Keys) {
            if (([string]$key) -ieq "Path") {
                [pscustomobject]@{
                    Key = [string]$key
                    Value = [string]$processEnvironment[$key]
                }
            }
        }
    )
    $normalized = New-Object `
        "System.Collections.Generic.Dictionary[string,string]" `
        ([StringComparer]::OrdinalIgnoreCase)
    foreach ($key in $processEnvironment.Keys) {
        if (([string]$key) -ine "Path") {
            $normalized[[string]$key] = [string]$processEnvironment[$key]
        }
    }
    if ($pathEntries.Count -gt 0) {
        # Prefer the longest entry: Codex normally extends the ordinary Windows
        # Path with its sandbox tools, so choosing the shorter value would
        # silently make executables disappear from child processes.
        $selectedEntry = $pathEntries |
            Sort-Object { $_.Value.Length } -Descending |
            Select-Object -First 1
        $normalized["Path"] = [string]$selectedEntry.Value
    }

    return [pscustomobject]@{
        Variables = $normalized
        PathVariantCount = $pathEntries.Count
        NormalizedPathCount = if ($normalized.ContainsKey("Path")) { 1 } else { 0 }
    }
}

function Set-MSBuildSdkFallback {
    param([Parameter(Mandatory = $true)][string]$MSBuildPath)

    # The lightweight Visual Studio Build Tools installation on this machine
    # has Roslyn/MSBuild but no bundled Microsoft.NET.Sdk resolver. Unity's
    # generated projects still import that SDK, so inherit the newest complete
    # SDK already installed by dotnet when MSBuildSDKsPath is not usable.
    $currentSdkPath = [Environment]::GetEnvironmentVariable(
        "MSBuildSDKsPath", [EnvironmentVariableTarget]::Process)
    if (-not [string]::IsNullOrWhiteSpace($currentSdkPath) -and
        (Test-Path -LiteralPath (Join-Path $currentSdkPath "Microsoft.NET.Sdk\Sdk\Sdk.props"))) {
        return $currentSdkPath
    }

    $sdkRoots = @(
        "C:\Program Files\dotnet\sdk",
        "C:\Program Files (x86)\dotnet\sdk"
    )
    $sdkPath = $sdkRoots |
        Where-Object { Test-Path -LiteralPath $_ } |
        ForEach-Object { Get-ChildItem -LiteralPath $_ -Directory } |
        Where-Object {
            Test-Path -LiteralPath (Join-Path $_.FullName "Sdks\Microsoft.NET.Sdk\Sdk\Sdk.props")
        } |
        Sort-Object {
            $parsedVersion = [version]"0.0"
            [version]::TryParse($_.Name, [ref]$parsedVersion) | Out-Null
            $parsedVersion
        } -Descending |
        Select-Object -First 1 -ExpandProperty FullName
    if (-not $sdkPath) {
        throw "$MSBuildPath requires Microsoft.NET.Sdk, but no complete dotnet SDK installation was found."
    }

    $resolvedSdkPath = Join-Path $sdkPath "Sdks"
    [Environment]::SetEnvironmentVariable(
        "MSBuildSDKsPath", $resolvedSdkPath, [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable(
        "MSBuildEnableWorkloadResolver", "false", [EnvironmentVariableTarget]::Process)
    return $resolvedSdkPath
}

$initialEnvironment = Get-NormalizedProcessEnvironment
$pathVariantCount = $initialEnvironment.PathVariantCount
$normalizedPathCount = $initialEnvironment.NormalizedPathCount

function Stop-OwnedProcessTree {
    param([Parameter(Mandatory = $true)][int]$ProcessId)

    $taskkillPath = "C:\Windows\System32\taskkill.exe"
    if (Test-Path -LiteralPath $taskkillPath) {
        & $taskkillPath /PID $ProcessId /T /F 2>$null | Out-Null
    }

    Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
}

function Invoke-BoundedProcess {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$FilePath,
        [string[]]$ArgumentList = @(),
        [Parameter(Mandatory = $true)][string]$WorkingDirectory,
        [Parameter(Mandatory = $true)][string]$LogDirectory,
        [Parameter(Mandatory = $true)][int]$HardTimeoutSeconds
    )

    $stamp = Get-Date -Format "yyyyMMdd-HHmmss-fff"
    $stdoutPath = Join-Path $LogDirectory "$Name-$stamp.out.log"
    $stderrPath = Join-Path $LogDirectory "$Name-$stamp.err.log"
    $exitCodePath = Join-Path $LogDirectory "$Name-$stamp.exit.txt"
    $childRunnerPath = Join-Path $PSScriptRoot "run-child.ps1"
    $runnerArguments = @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", $childRunnerPath,
        "-ToolPath", $FilePath,
        "-WorkingDirectory", $WorkingDirectory,
        "-ExitCodePath", $exitCodePath
    ) + $ArgumentList
    $runnerArguments = @(
        $runnerArguments | ForEach-Object {
            if ($_ -match "\s") {
                '"' + ($_ -replace '"', '\"') + '"'
            }
            else {
                $_
            }
        }
    )
    $processEnvironment = Get-NormalizedProcessEnvironment
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = "powershell.exe"
    $startInfo.Arguments = $runnerArguments -join " "
    $startInfo.WorkingDirectory = $WorkingDirectory
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.Environment.Clear()
    foreach ($entry in $processEnvironment.Variables.GetEnumerator()) {
        $startInfo.Environment[$entry.Key] = $entry.Value
    }

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    if (-not $process.Start()) {
        throw "$Name could not be launched."
    }
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()

    Write-Host "$Name PID: $($process.Id)"
    Write-Host "$Name stdout: $stdoutPath"
    Write-Host "$Name stderr: $stderrPath"

    $deadline = (Get-Date).AddSeconds($HardTimeoutSeconds)
    while (-not $process.HasExited -and (Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 2
        $process.Refresh()
    }

    if (-not $process.HasExited) {
        Stop-OwnedProcessTree -ProcessId $process.Id
        try { $process.WaitForExit() } catch { }
        $stdoutText = $stdoutTask.GetAwaiter().GetResult()
        $stderrText = $stderrTask.GetAwaiter().GetResult()
        [System.IO.File]::WriteAllText($stdoutPath, $stdoutText)
        [System.IO.File]::WriteAllText($stderrPath, $stderrText)
        Write-Host "$Name exceeded $HardTimeoutSeconds seconds." -ForegroundColor Red
        if (Test-Path -LiteralPath $stdoutPath) {
            Get-Content -LiteralPath $stdoutPath -Tail 80
        }
        if (Test-Path -LiteralPath $stderrPath) {
            Get-Content -LiteralPath $stderrPath -Tail 80
        }
        throw "$Name timed out."
    }

    $process.WaitForExit()
    $stdoutText = $stdoutTask.GetAwaiter().GetResult()
    $stderrText = $stderrTask.GetAwaiter().GetResult()
    [System.IO.File]::WriteAllText($stdoutPath, $stdoutText)
    [System.IO.File]::WriteAllText($stderrPath, $stderrText)
    if (-not (Test-Path -LiteralPath $exitCodePath)) {
        throw "$Name exited without an exit-code result file."
    }
    $exitCode = [int](Get-Content -Raw -LiteralPath $exitCodePath)
    if (Test-Path -LiteralPath $stdoutPath) {
        Get-Content -LiteralPath $stdoutPath -Tail 80
    }
    if (Test-Path -LiteralPath $stderrPath) {
        Get-Content -LiteralPath $stderrPath -Tail 80
    }
    if ($exitCode -ne 0) {
        throw "$Name failed with exit code $exitCode."
    }

    return @{
        ExitCode = $exitCode
        Stdout = $stdoutPath
        Stderr = $stderrPath
    }
}

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $PSScriptRoot "..\..\..\.."
}

$resolvedProject = (Resolve-Path -LiteralPath $ProjectPath).Path
$solutionPath = Join-Path $resolvedProject "MandateOfHeroes.sln"
$coreRunnerSource = Join-Path $resolvedProject "Tools\CoreTestRunner.cs"
$unityTestScript = Join-Path $resolvedProject "Tools\Run-UnityTestsSafe.ps1"
$binaryDirectory = Join-Path $resolvedProject "Temp\bin\Debug"
$coreRunnerPath = Join-Path $binaryDirectory "CoreTestRunner.exe"
$logDirectory = Join-Path $resolvedProject "tmp\skill-verification"
$projectVersionPath = Join-Path $resolvedProject "ProjectSettings\ProjectVersion.txt"
$unityEditorVersion = if (Test-Path -LiteralPath $projectVersionPath) {
    $versionMatch = Select-String -LiteralPath $projectVersionPath `
        -Pattern '^m_EditorVersion:\s*(\S+)' |
        Select-Object -First 1
    if ($versionMatch) { $versionMatch.Matches[0].Groups[1].Value } else { "" }
}
else {
    ""
}
$unityMonoPath = if ([string]::IsNullOrWhiteSpace($unityEditorVersion)) {
    ""
}
else {
    "C:\Program Files\Unity\Hub\Editor\$unityEditorVersion\Editor\Data\MonoBleedingEdge\bin\mono.exe"
}

New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null

Write-Host "Project: $resolvedProject"
Write-Host "Logs: $logDirectory"
Write-Host "Hard timeout per external tool: $TimeoutSeconds seconds"
Write-Host "Process Path variants before normalization: $pathVariantCount; child environment: $normalizedPathCount"

Push-Location $resolvedProject
try {
    if ($DocumentationOnly) {
        & git diff --check
        if ($LASTEXITCODE -ne 0) {
            throw "git diff --check failed."
        }
        Write-Host "RESULT documentation-only diff-check=passed"
        exit 0
    }

    $msbuildCandidates = @(
        "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    )
    $msbuildPath = $msbuildCandidates |
        Where-Object { Test-Path -LiteralPath $_ } |
        Select-Object -First 1
    if (-not $msbuildPath) {
        throw "MSBuild.exe was not found in the supported Visual Studio locations."
    }
    $msbuildSdkPath = Set-MSBuildSdkFallback -MSBuildPath $msbuildPath
    Write-Host "MSBuild: $msbuildPath"
    Write-Host "MSBuild SDKs: $msbuildSdkPath"

    Invoke-BoundedProcess `
        -Name "compile" `
        -FilePath $msbuildPath `
        -ArgumentList @(
            $solutionPath,
            "/restore",
            "/t:Build",
            "/p:Configuration=Debug",
            "/nologo",
            "/verbosity:minimal"
        ) `
        -WorkingDirectory $resolvedProject `
        -LogDirectory $logDirectory `
        -HardTimeoutSeconds $TimeoutSeconds | Out-Null

    $cscPath = Join-Path (Split-Path -Parent $msbuildPath) "Roslyn\csc.exe"
    if (-not (Test-Path -LiteralPath $cscPath)) {
        throw "Unity C# compiler was not found: $cscPath"
    }

    Invoke-BoundedProcess `
        -Name "core-runner-compile" `
        -FilePath $cscPath `
        -ArgumentList @(
            "/nologo",
            "/target:exe",
            "/out:$coreRunnerPath",
            $coreRunnerSource
        ) `
        -WorkingDirectory $resolvedProject `
        -LogDirectory $logDirectory `
        -HardTimeoutSeconds $TimeoutSeconds | Out-Null

    $nunitPath = Join-Path $resolvedProject (
        "Library\PackageCache\com.unity.ext.nunit@1.0.6\net35\unity-custom\nunit.framework.dll"
    )
    if (-not (Test-Path -LiteralPath $nunitPath)) {
        throw "Unity NUnit framework was not found: $nunitPath"
    }
    Copy-Item `
        -LiteralPath $nunitPath `
        -Destination (Join-Path $binaryDirectory "nunit.framework.dll") `
        -Force

    $coreArguments = @($resolvedProject, $binaryDirectory)
    if (-not [string]::IsNullOrWhiteSpace($CoreTestFilter)) {
        $coreArguments += $CoreTestFilter
    }
    $coreHostPath = $coreRunnerPath
    if (-not [string]::IsNullOrWhiteSpace($unityMonoPath) -and
        (Test-Path -LiteralPath $unityMonoPath)) {
        # Unity package assemblies can fail Windows CLR strong-name validation
        # even though they are valid in the project's Unity Mono runtime.
        $coreHostPath = $unityMonoPath
        $coreArguments = @($coreRunnerPath) + $coreArguments
        Write-Host "Core test runtime: $unityMonoPath"
    }
    $coreResult = Invoke-BoundedProcess `
        -Name "core-tests" `
        -FilePath $coreHostPath `
        -ArgumentList $coreArguments `
        -WorkingDirectory $resolvedProject `
        -LogDirectory $logDirectory `
        -HardTimeoutSeconds $TimeoutSeconds

    $coreSummary = Select-String `
        -LiteralPath $coreResult.Stdout `
        -Pattern "^RESULT passed=\d+ failed=0$"
    if (-not $coreSummary) {
        throw "Core tests exited successfully without a passing RESULT summary."
    }

    if (-not $SkipUnity) {
        $existingUnity = Get-Process -Name Unity -ErrorAction SilentlyContinue
        if ($null -ne $existingUnity) {
            $unityIds = ($existingUnity | Select-Object -ExpandProperty Id) -join ", "
            throw "Unity test blocked: an editor is already running (PID: $unityIds)."
        }

        $unityInnerTimeout = [Math]::Max(30, $TimeoutSeconds - 15)
        $unityMode = if ($UnityTestPlatform -eq "EditMode") {
            "EditModeTests"
        }
        else {
            "PlayModeTests"
        }
        $unityArguments = @(
            "-NoProfile",
            "-ExecutionPolicy", "Bypass",
            "-File", $unityTestScript,
            "-Mode", $unityMode,
            "-TimeoutSeconds", $unityInnerTimeout,
            "-ProjectPath", $resolvedProject
        )
        if (-not [string]::IsNullOrWhiteSpace($UnityTestFilter)) {
            $unityArguments += @("-TestFilter", $UnityTestFilter)
        }
        Invoke-BoundedProcess `
            -Name "unity-tests" `
            -FilePath "powershell.exe" `
            -ArgumentList $unityArguments `
            -WorkingDirectory $resolvedProject `
            -LogDirectory $logDirectory `
            -HardTimeoutSeconds $TimeoutSeconds | Out-Null
    }
    else {
        Write-Host "Unity tests: not run (-SkipUnity)."
    }

    & git diff --check
    if ($LASTEXITCODE -ne 0) {
        throw "git diff --check failed."
    }

    $unityStatus = if ($SkipUnity) { "not-run" } else { "passed" }
    Write-Host "RESULT compile=passed core-tests=passed unity-tests=$unityStatus diff-check=passed"
}
finally {
    Pop-Location
}
