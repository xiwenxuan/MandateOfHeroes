[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Script,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ScriptArguments
)

$ErrorActionPreference = 'Stop'

$qgisRoots = Get-ChildItem -LiteralPath 'C:\Program Files' -Directory -Filter 'QGIS *' -ErrorAction SilentlyContinue |
    Sort-Object Name -Descending
if (-not $qgisRoots) {
    throw 'QGIS was not found under C:\Program Files. Install the current QGIS LTR build first.'
}

$qgisRoot = $qgisRoots[0].FullName
$pythonHome = Join-Path $qgisRoot 'apps\Python312'
$qgisPython = Join-Path $qgisRoot 'apps\qgis-ltr\python'
$pythonExe = Join-Path $qgisRoot 'bin\python.exe'

foreach ($required in @($pythonHome, $qgisPython, $pythonExe)) {
    if (-not (Test-Path -LiteralPath $required)) {
        throw "Required QGIS runtime path is missing: $required"
    }
}

$env:PYTHONHOME = $pythonHome
$env:PYTHONPATH = "$qgisPython;$pythonHome\Lib\site-packages"
$env:PATH = "$qgisRoot\bin;$qgisRoot\apps\qgis-ltr\bin;$qgisRoot\apps\Qt5\bin;$qgisRoot\apps\gdal\bin;$env:PATH"
$env:PROJ_LIB = "$qgisRoot\share\proj"
$env:GDAL_DATA = "$qgisRoot\apps\gdal\share\gdal"
$matplotlibCache = Join-Path (Split-Path -Parent $PSScriptRoot) '.cache\matplotlib'
New-Item -ItemType Directory -Path $matplotlibCache -Force | Out-Null
$env:MPLCONFIGDIR = $matplotlibCache

$resolvedScript = (Resolve-Path -LiteralPath $Script).Path
& $pythonExe $resolvedScript @ScriptArguments
exit $LASTEXITCODE
