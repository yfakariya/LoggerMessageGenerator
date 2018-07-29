<#
.SYNOPSIS
Set version infomration in src/Package.props
#>

[string]$version = (Get-Content ./Version.txt);
[Version]$assemblyBaseVersion = $version | ForEach-Object { if ( $_ -match "^\d+\.\d+" ) { $matches[0] } } | New-Object Version($_)

[string]$packageVersion
if ($env:APPVEYOR_REPO_TAG -ne "True") {
    if ($null -eq ${env:APPVEYOR_BUILD_NUMBER}) {
        $now = [DateTime]::UtcNow
        $daysSpan = $now - ( New-Object DateTime( $now.Year, 1, 1 ) )
        $packageVersion = "${version}-preview{0:yy}{1:000}-{2:000}" -f @( $now, $daysSpan.Days, ( $now.TimeOfDay.TotalMinutes / 2 ) )
    }
    else {
        $packageVersion = "${version}-preview${env:APPVEYOR_BUILD_NUMBER}"
    }
}
elseif ($null -ne $env:APPVEYOR_REPO_TAG_NAME) {
    $packageVersion = $env:APPVEYOR_REPO_TAG_NAME
}
else {
    $packageVersion = $version
}

[string]$versionSuffix = ""
if ($null -ne $env:APPVEYOR_REPO_COMMIT) {
    $versionSuffix = $env:APPVEYOR_REPO_COMMIT
}

# Major = Informational-Major, Minor = Informational-Minor
# Build = Epoc days from 2010/1/1, Revision = Epoc minutes from 00:00:00
[DateTime]$today = [DateTime]::UtcNow;
[int]$build = [int](($today.Date - (New-Object DateTime(2010, 1, 1)).ToUniversalTime()).TotalDays);
[int]$revision = [int](($today - $today.Date).TotalMinutes);
if ($null -eq ${env:APPVEYOR_BUILD_NUMBER}) {
    $revision = $env:APPVEYOR_BUILD_NUMBER
}
[Version]$fileVersion = New-Object Version($version.Major, $version.Minor, $build, $revision);

Write-Host "AssemblyVersion:'$($assemblyBaseVersion.Major).$($assemblyBaseVersion.Minor).0.0', FileVersion:'${fileVersion}', PackageVersion:'${packageVersion}' Version:'${packageVersion}${versionSuffix}'"

[IO.File]::WriteAllText(
    "../../src/Package.props",
    @"
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
	<AssemblyVersion>$($assemblyBaseVersion.Major).$($assemblyBaseVersion.Minor).0.0</AssemblyVersion>
	<FileVersion>${fileVersion}</FileVersion>
	<PackageVersion>${packageVersion}</PackageVersion>
	<Version>${packageVersion}${versionSuffix}</Version>
  </PropertyGroup>
</Project>
"@
);
