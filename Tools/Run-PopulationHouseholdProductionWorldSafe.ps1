[CmdletBinding()]
param(
    [string]$ProjectPath,
    [ValidateRange(1182, 50000000)][int]$InitialLiving = 1000000,
    [ValidateRange(1, 200)][int]$Years = 50,
    [UInt64]$Seed = 14000024,
    [switch]$SelfTest,
    [switch]$Reset,
    [ValidateRange(30, 300)][int]$TimeoutSeconds = 300
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$arguments = @{
    InitialLiving = $InitialLiving
    Years = $Years
    Seed = $Seed
    HouseholdProduction = $true
    SelfTest = $SelfTest
    Reset = $Reset
    TimeoutSeconds = $TimeoutSeconds
}
if (-not [string]::IsNullOrWhiteSpace($ProjectPath)) {
    $arguments.ProjectPath = $ProjectPath
}

& (Join-Path $PSScriptRoot "Run-PopulationFiftyYearWorldSafe.ps1") @arguments
