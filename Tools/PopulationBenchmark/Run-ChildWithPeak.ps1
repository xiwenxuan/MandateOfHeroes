param(
    [Parameter(Mandatory = $true)][string]$ToolPath,
    [Parameter(Mandatory = $true)][string]$WorkingDirectory,
    [Parameter(Mandatory = $true)][string]$ExitCodePath,
    [Parameter(Mandatory = $true)][string]$MetricsPath,
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$ToolArguments
)

$ErrorActionPreference = "Stop"
$quoted = @($ToolArguments | ForEach-Object { if ($_ -match "\s") { '"' + ($_ -replace '"', '\"') + '"' } else { $_ } })
$started = (Get-Date).ToUniversalTime()
$peak = 0L
$child = $null
$exitCode = -1
try {
    $child = Start-Process -FilePath $ToolPath -ArgumentList $quoted -WorkingDirectory $WorkingDirectory -NoNewWindow -PassThru
    $tick = 0
    while (-not $child.HasExited) {
        $child.Refresh()
        if ($child.WorkingSet64 -gt $peak) { $peak = $child.WorkingSet64 }
        if (($tick % 4) -eq 0) {
            $partial = [ordered]@{
                schema_version = "m15.p3.child-metrics.v1"; child_pid = $child.Id
                started_at_utc = $started.ToString("o"); ended_at_utc = $null
                exit_code = $null; peak_working_set_bytes = $peak
            }
            [System.IO.File]::WriteAllText($MetricsPath, ($partial | ConvertTo-Json -Depth 4), (New-Object System.Text.UTF8Encoding($false)))
        }
        $tick++
        Start-Sleep -Milliseconds 500
    }
    $child.WaitForExit()
    $child.Refresh()
    if ($child.PeakWorkingSet64 -gt $peak) { $peak = $child.PeakWorkingSet64 }
    $exitCode = [int]$child.ExitCode
}
finally {
    $ended = (Get-Date).ToUniversalTime()
    if ($null -eq $exitCode) { $exitCode = -1 }
    [System.IO.File]::WriteAllText($ExitCodePath, ([int]$exitCode).ToString([System.Globalization.CultureInfo]::InvariantCulture))
    $metrics = [ordered]@{
        schema_version = "m15.p3.child-metrics.v1"
        child_pid = if ($null -eq $child) { $null } else { $child.Id }
        started_at_utc = $started.ToString("o")
        ended_at_utc = $ended.ToString("o")
        exit_code = $exitCode
        peak_working_set_bytes = $peak
    }
    [System.IO.File]::WriteAllText($MetricsPath, ($metrics | ConvertTo-Json -Depth 4), (New-Object System.Text.UTF8Encoding($false)))
}
exit $exitCode
