param(
    [Parameter(Mandatory = $true)][string]$ToolPath,
    [Parameter(Mandatory = $true)][string]$WorkingDirectory,
    [Parameter(Mandatory = $true)][string]$ExitCodePath,
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$ToolArguments
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"

Push-Location $WorkingDirectory
try {
    & $ToolPath @ToolArguments
    $code = $LASTEXITCODE
    if ($null -eq $code) {
        $code = if ($?) { 0 } else { 1 }
    }
    [System.IO.File]::WriteAllText(
        $ExitCodePath,
        ([string]$code),
        (New-Object System.Text.UTF8Encoding($false)))
    exit $code
}
finally {
    Pop-Location
}
