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

$resolvedProject = if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
} else { (Resolve-Path -LiteralPath $ProjectPath).Path }
$arguments = @{
    ProjectPath = $resolvedProject
    InitialLiving = $InitialLiving
    Years = $Years
    Seed = $Seed
    PopulationResourceCalibration = $true
    SubsistenceProfilePath = Join-Path $resolvedProject "Data\PopulationSimulation\subsistence_pressure_profile.han140_calibration_candidate3.v1.json"
    HouseholdProductionProfilePath = Join-Path $resolvedProject "Data\PopulationSimulation\household_production_profile.han140_calibration_candidate.v1.json"
    PopulationResourceCalibrationProfilePath = Join-Path $resolvedProject "Data\PopulationSimulation\population_resource_calibration_profile.han140_candidate.v1.json"
    SelfTest = $SelfTest
    Reset = $Reset
    TimeoutSeconds = $TimeoutSeconds
}

& (Join-Path $PSScriptRoot "Run-PopulationFiftyYearWorldSafe.ps1") @arguments
