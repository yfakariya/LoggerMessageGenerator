<#
.SYNOPSIS
Build script.

.PARAMETER Rebuild
If specified, rebuild solution.
#>

param([Switch]$Rebuild)

[string]$msbuild = "msbuild"

./GeneratePackageProps.ps1

[string]$buildConfig = 'Release'
if (![String]::IsNullOrWhitespace($env:CONFIGURATION)) {
    $buildConfig = $env:CONFIGURATION
}

[string]$sln = '../../Wisteria.LoggerMessageGenerator.sln'

$buildOptions = @('/v:minimal')
if ($Rebuild) {
    $buildOptions += '/t:Rebuild'
}

$buildOptions += "/p:Configuration=${buildConfig}"
$restoreOptions = "/v:minimal"

Write-Host "Clean up directories..."

if (!(Test-Path "../dist")) {
    New-Item ../dist -Type Directory | Out-Null
}
else {
    Remove-Item ../dist/* -Recurse -Force
}

# build

Write-Host "Restore $sln packages..."

& $msbuild /t:restore $sln $restoreOptions
if ($LastExitCode -ne 0) {
    Write-Error "Failed to restore $sln"
    exit $LastExitCode
}

Write-Host "Build $sln..."

& $msbuild $sln $buildOptions
if ($LastExitCode -ne 0) {
    Write-Error "Failed to build $sln"
    exit $LastExitCode
}

if ($buildConfig -eq 'Release') {
    Write-Host "Build NuGet packages..."

    & $msbuild ../../src/Wisteria.LoggerMessageGenerator/Wisteria.LoggerMessageGenerator.csproj /t:pack /v:minimal /p:Configuration=$buildConfig /p:IncludeSource=true /p:NuspecProperties=version=$env:PackageVersion

    Move-Item ../bin/*.nupkg ../dist/
}
