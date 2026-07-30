param(
    [Parameter(Mandatory = $true)]
    [string]$ToolPath,
    [Parameter(Mandatory = $true)]
    [string]$WorkingDirectory,
    [Parameter(Mandatory = $true)]
    [string]$ExitCodePath,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ToolArguments
)

$exitCode = 1
try {
    Push-Location $WorkingDirectory
    & $ToolPath @ToolArguments
    $exitCode = $LASTEXITCODE
    if ($null -eq $exitCode) {
        $exitCode = if ($?) { 0 } else { 1 }
    }
}
catch {
    Write-Error $_
    $exitCode = 1
}
finally {
    Pop-Location
    [System.IO.File]::WriteAllText(
        $ExitCodePath,
        [string]$exitCode,
        [System.Text.Encoding]::ASCII
    )
}

exit $exitCode
