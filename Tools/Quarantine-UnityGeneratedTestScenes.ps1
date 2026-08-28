[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$ProjectPath = '',
    [string]$QuarantineRoot = 'tmp\workspace-quarantine\unity-generated-test-scenes'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Split-Path -Parent $scriptDirectory
}

$resolvedProject = (Resolve-Path -LiteralPath $ProjectPath).Path
$assetsPath = (Resolve-Path -LiteralPath (Join-Path $resolvedProject 'Assets')).Path
$resolvedQuarantineRoot = [System.IO.Path]::GetFullPath((Join-Path $resolvedProject $QuarantineRoot))
$projectPrefix = $resolvedProject.TrimEnd('\') + '\'

if (-not $assetsPath.StartsWith($projectPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Assets path is outside the project: $assetsPath"
}
if (-not $resolvedQuarantineRoot.StartsWith($projectPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Quarantine path is outside the project: $resolvedQuarantineRoot"
}

$scenes = @(
    Get-ChildItem -LiteralPath $assetsPath -File -Filter 'InitTestScene*.unity' |
        Where-Object Name -Match '^InitTestScene\d+\.unity$'
)

if ($scenes.Count -eq 0) {
    Write-Host 'No generated Unity Test Framework bootstrap scenes were found.'
    exit 0
}

$validatedFiles = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
foreach ($scene in $scenes) {
    $relativeScene = 'Assets/' + $scene.Name
    $trackedScene = @(git -C $resolvedProject ls-files -- $relativeScene)
    if ($trackedScene.Count -gt 0) {
        throw "Refusing to quarantine tracked scene: $relativeScene"
    }

    $sceneText = Get-Content -Raw -LiteralPath $scene.FullName
    if ($sceneText -notmatch 'm_Name:\s+Code-based tests runner' -or
        $sceneText -notmatch ('bootstrapScene:\s+Assets/' + [regex]::Escape($scene.Name))) {
        throw "Scene does not match the Unity Test Framework bootstrap signature: $relativeScene"
    }

    $validatedFiles.Add($scene)
    $metaPath = $scene.FullName + '.meta'
    if (Test-Path -LiteralPath $metaPath -PathType Leaf) {
        $relativeMeta = $relativeScene + '.meta'
        $trackedMeta = @(git -C $resolvedProject ls-files -- $relativeMeta)
        if ($trackedMeta.Count -gt 0) {
            throw "Refusing to quarantine tracked meta file: $relativeMeta"
        }
        $validatedFiles.Add((Get-Item -LiteralPath $metaPath))
    }
}

$batchName = Get-Date -Format 'yyyyMMdd-HHmmss'
$destination = Join-Path $resolvedQuarantineRoot $batchName

if ($PSCmdlet.ShouldProcess($destination, "Move $($validatedFiles.Count) generated scene/meta files out of Assets")) {
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    $manifestRows = [System.Collections.Generic.List[object]]::new()
    foreach ($file in $validatedFiles) {
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $targetPath = Join-Path $destination $file.Name
        Move-Item -LiteralPath $file.FullName -Destination $targetPath
        $manifestRows.Add([ordered]@{
            source = $file.FullName.Substring($resolvedProject.Length + 1).Replace('\', '/')
            quarantined = $targetPath.Substring($resolvedProject.Length + 1).Replace('\', '/')
            sha256 = $hash
        })
    }

    $manifest = [ordered]@{
        schema = 'mandate-of-heroes.workspace-quarantine.v1'
        reason = 'Interrupted Unity Test Framework bootstrap scenes removed from Assets without deletion.'
        files = @($manifestRows)
    }
    $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $destination 'quarantine-manifest.json') -Encoding utf8
    Write-Host "Quarantined $($validatedFiles.Count) files to: $destination"
}
