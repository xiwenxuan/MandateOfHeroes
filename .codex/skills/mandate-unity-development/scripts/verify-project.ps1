[CmdletBinding()]
param(
    [string]$ProjectPath,
    [ValidateRange(30, 1800)]
    [int]$TimeoutSeconds = 300,
    [ValidateSet("EditMode", "PlayMode")]
    [string]$UnityTestPlatform = "EditMode",
    [switch]$SkipUnity,
    [switch]$DocumentationOnly
)

$ErrorActionPreference = "Stop"

# PowerShell 5 Start-Process fails when the host exposes both Path and PATH.
$processPath = $env:Path
[Environment]::SetEnvironmentVariable("PATH", $null, "Process")
[Environment]::SetEnvironmentVariable("Path", $processPath, "Process")

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
    $process = Start-Process `
        -FilePath "powershell.exe" `
        -ArgumentList $runnerArguments `
        -WorkingDirectory $WorkingDirectory `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath `
        -PassThru `
        -WindowStyle Hidden

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

New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null

Write-Host "Project: $resolvedProject"
Write-Host "Logs: $logDirectory"
Write-Host "Hard timeout per external tool: $TimeoutSeconds seconds"

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
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    )
    $msbuildPath = $msbuildCandidates |
        Where-Object { Test-Path -LiteralPath $_ } |
        Select-Object -First 1
    if (-not $msbuildPath) {
        throw "MSBuild.exe was not found in the supported Visual Studio locations."
    }

    Invoke-BoundedProcess `
        -Name "compile" `
        -FilePath $msbuildPath `
        -ArgumentList @(
            $solutionPath,
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

    $coreResult = Invoke-BoundedProcess `
        -Name "core-tests" `
        -FilePath $coreRunnerPath `
        -ArgumentList @($resolvedProject, $binaryDirectory) `
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
        Invoke-BoundedProcess `
            -Name "unity-tests" `
            -FilePath "powershell.exe" `
            -ArgumentList @(
                "-NoProfile",
                "-ExecutionPolicy", "Bypass",
                "-File", $unityTestScript,
                "-TestPlatform", $UnityTestPlatform,
                "-TimeoutSeconds", $unityInnerTimeout,
                "-ProjectPath", $resolvedProject
            ) `
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
