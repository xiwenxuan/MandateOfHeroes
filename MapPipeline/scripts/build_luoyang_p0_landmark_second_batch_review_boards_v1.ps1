param(
    [string]$SourceDirectory = "",
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
$taskRelativeRoot = "Docs\HISTORICAL_WORLD_REFERENCE\LUOYANG_P0_LANDMARK_SECOND_BATCH_MULTI_ANGLE_REVIEW_AND_DECISION_BOARDS_V1"
if ([string]::IsNullOrWhiteSpace($SourceDirectory)) {
    $SourceDirectory = Join-Path $projectRoot (Join-Path $taskRelativeRoot "Screenshots")
}
elseif (-not [System.IO.Path]::IsPathRooted($SourceDirectory)) {
    $SourceDirectory = Join-Path $projectRoot $SourceDirectory
}
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $projectRoot $taskRelativeRoot
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $projectRoot $OutputDirectory
}

$sourceRoot = (Resolve-Path -LiteralPath $SourceDirectory).Path
[System.IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null
$outputRoot = (Resolve-Path -LiteralPath $OutputDirectory).Path
$boardRoot = Join-Path $outputRoot "Boards"
$machineRoot = Join-Path $outputRoot "Machine"
[System.IO.Directory]::CreateDirectory($boardRoot) | Out-Null
[System.IO.Directory]::CreateDirectory($machineRoot) | Out-Null

Add-Type -AssemblyName System.Drawing

function New-ReviewFont {
    param(
        [float]$Size,
        [System.Drawing.FontStyle]$Style = [System.Drawing.FontStyle]::Regular
    )
    try {
        return [System.Drawing.Font]::new(
            "Microsoft YaHei UI", $Size, $Style, [System.Drawing.GraphicsUnit]::Pixel)
    }
    catch {
        return [System.Drawing.Font]::new(
            "Arial", $Size, $Style, [System.Drawing.GraphicsUnit]::Pixel)
    }
}

function Get-RelativeProjectPath {
    param([string]$Path)

    $resolved = [System.IO.Path]::GetFullPath($Path)
    $prefix = $projectRoot.TrimEnd('\') + '\'
    if (-not $resolved.StartsWith(
            $prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Path is outside project root: $resolved"
    }
    return $resolved.Substring($prefix.Length).Replace('\', '/')
}

$pieces = @(
    [pscustomobject]@{
        Id = "candidate.luoyang.p0.batch2.north_palace.v1"
        Slug = "north_palace"
        Title = "NORTH PALACE"
        Check = "CHECK: TWIN QUE / HIGH TERRACE | REAR ENCLOSURE | STEPS / COLUMNS / EAVES"
    },
    [pscustomobject]@{
        Id = "candidate.luoyang.p0.batch2.yongan_palace.v1"
        Slug = "yongan_palace"
        Title = "YONGAN PALACE"
        Check = "CHECK: GARDEN COURT / POND | REAR ENCLOSURE | ASYMMETRIC PALACE IDENTITY"
    },
    [pscustomobject]@{
        Id = "candidate.luoyang.p0.batch2.taixue.v1"
        Slug = "taixue"
        Title = "TAIXUE"
        Check = "CHECK: LECTURE ROWS | COURTYARD DEPTH | ACADEMY, NOT PALACE"
    },
    [pscustomobject]@{
        Id = "candidate.luoyang.p0.batch2.biyong.v1"
        Slug = "biyong"
        Title = "BIYONG"
        Check = "CHECK: WATER RING | BRIDGE AXIS | CENTRAL RITUAL HALL"
    }
)
$angles = @(
    [pscustomobject]@{ Slug = "front_oblique"; Label = "FRONT OBLIQUE" },
    [pscustomobject]@{ Slug = "rear_oblique"; Label = "REAR OBLIQUE" },
    [pscustomobject]@{ Slug = "low_oblique"; Label = "LOW OBLIQUE" }
)

$canvasWidth = 3000
$canvasHeight = 900
$margin = 36
$gap = 24
$panelWidth = 960
$panelHeight = 600
$imageY = 152
$manifestPieces = [System.Collections.Generic.List[object]]::new()
$seenSourcePaths = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)
$generatedBoardPaths = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)

$titleFont = New-ReviewFont 38 ([System.Drawing.FontStyle]::Bold)
$subtitleFont = New-ReviewFont 22
$angleFont = New-ReviewFont 25 ([System.Drawing.FontStyle]::Bold)
$footerFont = New-ReviewFont 24
$decisionFont = New-ReviewFont 27 ([System.Drawing.FontStyle]::Bold)
$whiteBrush = [System.Drawing.SolidBrush]::new(
    [System.Drawing.Color]::FromArgb(242, 244, 239))
$mutedBrush = [System.Drawing.SolidBrush]::new(
    [System.Drawing.Color]::FromArgb(183, 192, 196))
$goldBrush = [System.Drawing.SolidBrush]::new(
    [System.Drawing.Color]::FromArgb(224, 184, 92))
$borderPen = [System.Drawing.Pen]::new(
    [System.Drawing.Color]::FromArgb(116, 128, 132), 3)

try {
    foreach ($piece in $pieces) {
        $sources = [System.Collections.Generic.List[object]]::new()
        $sourceImages = [System.Collections.Generic.List[System.Drawing.Image]]::new()
        try {
            foreach ($angle in $angles) {
                $fileName = "luoyang_p0_batch2_$($piece.Slug)_$($angle.Slug)_v1.png"
                $sourcePath = Join-Path $sourceRoot $fileName
                if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
                    throw "Missing review source image: $sourcePath"
                }
                if (-not $seenSourcePaths.Add($sourcePath)) {
                    throw "Duplicate review source image: $sourcePath"
                }
                $sourceImage = [System.Drawing.Image]::FromFile($sourcePath)
                if ($sourceImage.Width -ne 1600 -or $sourceImage.Height -ne 1000) {
                    $sourceImage.Dispose()
                    throw "Source image must be 1600x1000: $sourcePath"
                }
                $sourceImages.Add($sourceImage)
                $sources.Add([ordered]@{
                    angle = $angle.Slug
                    path = Get-RelativeProjectPath $sourcePath
                    width = $sourceImage.Width
                    height = $sourceImage.Height
                    sha256 = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash.ToLowerInvariant()
                })
            }

            $bitmap = [System.Drawing.Bitmap]::new(
                $canvasWidth, $canvasHeight,
                [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
            try {
                $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
                try {
                    $graphics.Clear([System.Drawing.Color]::FromArgb(24, 28, 31))
                    $graphics.CompositingQuality =
                        [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
                    $graphics.InterpolationMode =
                        [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                    $graphics.SmoothingMode =
                        [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                    $graphics.PixelOffsetMode =
                        [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                    $graphics.DrawString(
                        $piece.Title, $titleFont, $whiteBrush, [float]$margin, 18.0)
                    $graphics.DrawString(
                        "P0 LANDMARK BATCH 2 REVIEW DECISION BOARD V1 | UNITY GAME VIEW 1600x1000 | NO CROP / NO COLOR CHANGE",
                        $subtitleFont, $mutedBrush, [float]$margin, 70.0)

                    for ($index = 0; $index -lt $angles.Count; $index++) {
                        $x = $margin + ($index * ($panelWidth + $gap))
                        $graphics.DrawString(
                            $angles[$index].Label, $angleFont, $goldBrush, [float]$x, 112.0)
                        $destination = [System.Drawing.Rectangle]::new(
                            $x, $imageY, $panelWidth, $panelHeight)
                        $graphics.DrawImage($sourceImages[$index], $destination)
                        $graphics.DrawRectangle($borderPen, $destination)
                    }

                    $graphics.DrawString(
                        $piece.Check, $footerFont, $whiteBrush, [float]$margin, 772.0)
                    $graphics.DrawString(
                        "DECISION: PENDING   [ ACCEPT / CHANGE / REJECT ]        FINAL ART APPROVAL: FALSE",
                        $decisionFont, $goldBrush, [float]$margin, 824.0)
                }
                finally {
                    $graphics.Dispose()
                }

                $boardPath = Join-Path $boardRoot `
                    "luoyang_p0_batch2_$($piece.Slug)_review_decision_board_v1.png"
                $bitmap.Save($boardPath, [System.Drawing.Imaging.ImageFormat]::Png)
                if (-not $generatedBoardPaths.Add($boardPath)) {
                    throw "Duplicate generated board path: $boardPath"
                }
                $decoded = [System.Drawing.Image]::FromFile($boardPath)
                try {
                    if ($decoded.Width -ne $canvasWidth -or
                        $decoded.Height -ne $canvasHeight) {
                        throw "Generated board has invalid dimensions: $boardPath"
                    }
                }
                finally {
                    $decoded.Dispose()
                }
                $manifestPieces.Add([ordered]@{
                    piece_id = $piece.Id
                    display_name = $piece.Title
                    decision = "PENDING"
                    final_art_approved = $false
                    sources = $sources
                    board = [ordered]@{
                        path = Get-RelativeProjectPath $boardPath
                        width = $canvasWidth
                        height = $canvasHeight
                        sha256 = (Get-FileHash -LiteralPath $boardPath -Algorithm SHA256).Hash.ToLowerInvariant()
                    }
                })
            }
            finally {
                $bitmap.Dispose()
            }
        }
        finally {
            foreach ($sourceImage in $sourceImages) {
                $sourceImage.Dispose()
            }
        }
    }
}
finally {
    $titleFont.Dispose()
    $subtitleFont.Dispose()
    $angleFont.Dispose()
    $footerFont.Dispose()
    $decisionFont.Dispose()
    $whiteBrush.Dispose()
    $mutedBrush.Dispose()
    $goldBrush.Dispose()
    $borderPen.Dispose()
}

if ($seenSourcePaths.Count -ne 12 -or $manifestPieces.Count -ne 4) {
    throw "Decision-board coverage failed: sources=$($seenSourcePaths.Count), pieces=$($manifestPieces.Count)"
}
$boardFiles = @(Get-ChildItem -LiteralPath $boardRoot -Filter "*.png" -File)
if ($boardFiles.Count -ne 4) {
    throw "Board directory must contain exactly four PNG files: $boardRoot"
}
foreach ($boardFile in $boardFiles) {
    if (-not $generatedBoardPaths.Contains($boardFile.FullName)) {
        throw "Unexpected board file: $($boardFile.FullName)"
    }
}
foreach ($pieceRecord in $manifestPieces) {
    foreach ($sourceRecord in $pieceRecord.sources) {
        $sourcePath = Join-Path $projectRoot ($sourceRecord.path.Replace('/', '\'))
        $currentHash = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($currentHash -ne $sourceRecord.sha256) {
            throw "Source image changed during board generation: $sourcePath"
        }
    }
}

$manifest = [ordered]@{
    schema_version = 1
    contract_id = "presentation.luoyang.p0-landmark-second-batch.review-decision-board.v1"
    status = "LUOYANG_P0_LANDMARK_SECOND_BATCH_MULTI_ANGLE_DECISION_BOARDS_READY_FOR_USER_DECISION_V1"
    source_contract_id = "presentation.luoyang.p0-landmark-second-batch.multi-angle-review.v1"
    source_count = $seenSourcePaths.Count
    board_count = $manifestPieces.Count
    source_edit_policy = "NO_CROP_NO_COLOR_CHANGE_LAYOUT_ONLY"
    pieces = $manifestPieces
}
$manifestPath = Join-Path $machineRoot `
    "luoyang_p0_landmark_second_batch_review_decision_board_manifest_v1.json"
$json = $manifest | ConvertTo-Json -Depth 8
[System.IO.File]::WriteAllText(
    $manifestPath, $json + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false))

Write-Output "RESULT status=passed pieces=4 sources=12 boards=4"
Write-Output "Boards: $boardRoot"
Write-Output "Manifest: $manifestPath"
