[CmdletBinding()]
param(
    [string]$ProjectPath = '',
    [switch]$Json
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Split-Path -Parent $scriptDirectory
}

$errors = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()
$notes = [System.Collections.Generic.List[string]]::new()

function Add-ErrorMessage {
    param([string]$Message)
    $errors.Add($Message)
}

function Add-WarningMessage {
    param([string]$Message)
    $warnings.Add($Message)
}

function Add-NoteMessage {
    param([string]$Message)
    $notes.Add($Message)
}

try {
    $resolvedProject = (Resolve-Path -LiteralPath $ProjectPath).Path
}
catch {
    Write-Error "Project path does not exist: $ProjectPath"
    exit 1
}

foreach ($requiredDirectory in @('Assets', 'Packages', 'ProjectSettings')) {
    $requiredPath = Join-Path $resolvedProject $requiredDirectory
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Container)) {
        Add-ErrorMessage "Missing Unity project directory: $requiredDirectory"
    }
}

$projectVersionPath = Join-Path $resolvedProject 'ProjectSettings\ProjectVersion.txt'
$unityVersion = $null
if (-not (Test-Path -LiteralPath $projectVersionPath -PathType Leaf)) {
    Add-ErrorMessage 'Missing ProjectSettings/ProjectVersion.txt.'
}
else {
    $versionMatch = Select-String -LiteralPath $projectVersionPath -Pattern '^m_EditorVersion:\s*(\S+)' | Select-Object -First 1
    if ($null -eq $versionMatch) {
        Add-ErrorMessage 'ProjectVersion.txt does not declare m_EditorVersion.'
    }
    else {
        $unityVersion = $versionMatch.Matches[0].Groups[1].Value
        Add-NoteMessage "Unity version: $unityVersion"
    }
}

$unityEditorPath = $null
if (-not [string]::IsNullOrWhiteSpace($unityVersion)) {
    $unityEditorPath = "C:\Program Files\Unity\Hub\Editor\$unityVersion\Editor\Unity.exe"
    if (Test-Path -LiteralPath $unityEditorPath -PathType Leaf) {
        Add-NoteMessage "Matching Unity editor: $unityEditorPath"
    }
    else {
        Add-ErrorMessage "Matching Unity editor is not installed at: $unityEditorPath"
    }
}

foreach ($jsonRelativePath in @('Packages\manifest.json', 'Packages\packages-lock.json')) {
    $jsonPath = Join-Path $resolvedProject $jsonRelativePath
    if (-not (Test-Path -LiteralPath $jsonPath -PathType Leaf)) {
        Add-ErrorMessage "Missing package file: $jsonRelativePath"
        continue
    }

    try {
        Get-Content -Raw -LiteralPath $jsonPath | ConvertFrom-Json | Out-Null
        Add-NoteMessage "$jsonRelativePath is valid JSON."
    }
    catch {
        Add-ErrorMessage "$jsonRelativePath is not valid JSON: $($_.Exception.Message)"
    }
}

$buildSettingsPath = Join-Path $resolvedProject 'ProjectSettings\EditorBuildSettings.asset'
$missingBuildScenes = [System.Collections.Generic.List[string]]::new()
if (Test-Path -LiteralPath $buildSettingsPath -PathType Leaf) {
    $sceneMatches = Select-String -LiteralPath $buildSettingsPath -Pattern '^\s+path:\s+(.+\.unity)\s*$'
    foreach ($sceneMatch in $sceneMatches) {
        $relativeScenePath = $sceneMatch.Matches[0].Groups[1].Value.Trim()
        $absoluteScenePath = Join-Path $resolvedProject ($relativeScenePath.Replace('/', '\'))
        if (-not (Test-Path -LiteralPath $absoluteScenePath -PathType Leaf)) {
            $missingBuildScenes.Add($relativeScenePath)
        }
    }

    if ($missingBuildScenes.Count -gt 0) {
        Add-ErrorMessage "Build Settings references $($missingBuildScenes.Count) missing scene(s)."
    }
    else {
        Add-NoteMessage "Build Settings scene references are present ($($sceneMatches.Count))."
    }
}
else {
    Add-ErrorMessage 'Missing ProjectSettings/EditorBuildSettings.asset.'
}

$assetsPath = Join-Path $resolvedProject 'Assets'
$missingMeta = [System.Collections.Generic.List[string]]::new()
$orphanMeta = [System.Collections.Generic.List[string]]::new()
$duplicateGuidGroups = @()

if (Test-Path -LiteralPath $assetsPath -PathType Container) {
    $assetItems = Get-ChildItem -LiteralPath $assetsPath -Recurse -Force -ErrorAction Stop
    foreach ($assetItem in $assetItems) {
        if ($assetItem.Name.StartsWith('.') -or $assetItem.Name.EndsWith('.meta')) {
            continue
        }

        if (-not (Test-Path -LiteralPath ($assetItem.FullName + '.meta'))) {
            $missingMeta.Add($assetItem.FullName.Substring($assetsPath.Length + 1))
        }
    }

    $guidRows = [System.Collections.Generic.List[object]]::new()
    foreach ($metaFile in ($assetItems | Where-Object { -not $_.PSIsContainer -and $_.Name.EndsWith('.meta') })) {
        $targetPath = $metaFile.FullName.Substring(0, $metaFile.FullName.Length - 5)
        if (-not (Test-Path -LiteralPath $targetPath)) {
            $orphanMeta.Add($metaFile.FullName.Substring($assetsPath.Length + 1))
        }

        $guidMatch = Select-String -LiteralPath $metaFile.FullName -Pattern '^guid:\s*([0-9a-fA-F]+)\s*$' | Select-Object -First 1
        if ($null -ne $guidMatch) {
            $guidRows.Add([pscustomobject]@{
                Guid = $guidMatch.Matches[0].Groups[1].Value.ToLowerInvariant()
                Path = $metaFile.FullName.Substring($assetsPath.Length + 1)
            })
        }
    }

    $duplicateGuidGroups = @($guidRows | Group-Object Guid | Where-Object Count -gt 1)

    if ($missingMeta.Count -gt 0) {
        Add-ErrorMessage "Assets contains $($missingMeta.Count) item(s) without a matching .meta file."
    }
    if ($orphanMeta.Count -gt 0) {
        Add-ErrorMessage "Assets contains $($orphanMeta.Count) orphan .meta file(s)."
    }
    if ($duplicateGuidGroups.Count -gt 0) {
        Add-ErrorMessage "Assets contains $($duplicateGuidGroups.Count) duplicate GUID group(s)."
    }
    if ($missingMeta.Count -eq 0 -and $orphanMeta.Count -eq 0 -and $duplicateGuidGroups.Count -eq 0) {
        Add-NoteMessage 'Assets .meta/GUID integrity check passed.'
    }
}

$generatedTestScenes = @(
    Get-ChildItem -LiteralPath $assetsPath -File -Filter 'InitTestScene*.unity' -ErrorAction SilentlyContinue |
        Where-Object Name -Match '^InitTestScene\d+\.unity$' |
        Select-Object -ExpandProperty FullName
)
if ($generatedTestScenes.Count -gt 0) {
    Add-WarningMessage "Assets contains $($generatedTestScenes.Count) generated Unity Test Framework bootstrap scene(s). Run Tools/Quarantine-UnityGeneratedTestScenes.ps1 before normal editor work."
}

$unityLockPath = Join-Path $resolvedProject 'Temp\UnityLockfile'
if (Test-Path -LiteralPath $unityLockPath) {
    Add-WarningMessage 'Temp/UnityLockfile exists; the project may already be open or may have exited uncleanly.'
}

$unityProcesses = @(Get-Process -Name Unity -ErrorAction SilentlyContinue)
if ($unityProcesses.Count -gt 0) {
    Add-WarningMessage "$($unityProcesses.Count) Unity process(es) are currently running."
}

$result = [ordered]@{
    projectPath = $resolvedProject
    unityVersion = $unityVersion
    unityEditorPath = $unityEditorPath
    errors = @($errors)
    warnings = @($warnings)
    notes = @($notes)
    missingBuildScenes = @($missingBuildScenes)
    missingMeta = @($missingMeta)
    orphanMeta = @($orphanMeta)
    duplicateGuids = @($duplicateGuidGroups | ForEach-Object {
        [ordered]@{
            guid = $_.Name
            paths = @($_.Group | Select-Object -ExpandProperty Path)
        }
    })
    generatedTestScenes = @($generatedTestScenes)
}

if ($Json) {
    $result | ConvertTo-Json -Depth 6
}
else {
    Write-Host "Unity project preflight: $resolvedProject"
    foreach ($note in $notes) {
        Write-Host "[OK] $note"
    }
    foreach ($warning in $warnings) {
        Write-Warning $warning
    }
    foreach ($errorMessage in $errors) {
        Write-Host "[ERROR] $errorMessage" -ForegroundColor Red
    }
    Write-Host "Summary: errors=$($errors.Count) warnings=$($warnings.Count)"
}

if ($errors.Count -gt 0) {
    exit 1
}

exit 0
