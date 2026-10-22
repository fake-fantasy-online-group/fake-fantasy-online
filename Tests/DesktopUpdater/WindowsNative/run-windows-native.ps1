$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'This test requires real Windows APIs and NTFS; it is not an emulation test.' }
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$output = Join-Path ([IO.Path]::GetTempPath()) ('ffo-native-build-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output | Out-Null
function Invoke-Dotnet([string[]] $Arguments) {
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed ($LASTEXITCODE): $Arguments" }
}
function Escape-Xml([string] $Value) { [Security.SecurityElement]::Escape($Value) }
try {
    # Build the real production CLI separately, without adding test entry points.
    Invoke-Dotnet -Arguments @('publish', (Join-Path $root 'Tools/DesktopUpdater/DesktopUpdater.csproj'), '-c', 'Release', '-r', 'win-x64', '-o', (Join-Path $output 'helper'))

    $fixtureProject = Join-Path $output 'Fixture.csproj'
    $fixtureSource = Escape-Xml (Join-Path $PSScriptRoot 'FixtureApplication.cs')
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
    <AssemblyName>FixtureGame</AssemblyName><EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion>
  </PropertyGroup>
  <ItemGroup><Compile Include="$fixtureSource" /></ItemGroup>
</Project>
"@ | Set-Content -Encoding utf8 $fixtureProject
    foreach ($version in @('1.0.0', '1.0.1')) {
        Invoke-Dotnet -Arguments @('publish', $fixtureProject, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false', "-p:Version=$version", "-p:InformationalVersion=$version", '-o', (Join-Path $output $version))
    }

    # Compile the harness against exactly the source graph of the production project,
    # including its real internal DesktopPlatform. No test doubles or production edits.
    $production = Join-Path $root 'Tools/DesktopUpdater/DesktopUpdater.csproj'
    [xml] $project = Get-Content -Raw $production
    $sources = @(Get-ChildItem (Split-Path $production) -Recurse -Filter '*.cs' | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } | ForEach-Object { $_.FullName })
    foreach ($node in $project.SelectNodes('//*[@Condition]')) {
        if ($node.LocalName -eq 'Compile' -or $node.SelectSingleNode('.//Compile')) { throw 'Conditional production Compile items require an explicit test update.' }
    }
    if ($project.SelectSingleNode('//EnableDefaultItems|//EnableDefaultCompileItems')) { throw 'Unexpected production default source configuration.' }
    foreach ($item in $project.SelectNodes('//Compile')) {
        if (-not $item.Include -or $item.Include -match '[*?;$]' -or $item.HasAttribute('Exclude') -or $item.HasAttribute('Remove') -or $item.HasAttribute('Update')) { throw 'Production Compile items must be literal paths.' }
        $sources += (Resolve-Path (Join-Path (Split-Path $production) $item.Include)).Path
    }
    $sources += Join-Path $PSScriptRoot 'WindowsNativeContract.cs'
    $compile = ($sources | Select-Object -Unique | ForEach-Object { '<Compile Include="' + (Escape-Xml $_) + '" />' }) -join "`n"
    $harnessProject = Join-Path $output 'NativeContract.csproj'
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
    <StartupObject>WindowsNativeContract</StartupObject><EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable>
  </PropertyGroup>
  <ItemGroup>$compile</ItemGroup>
</Project>
"@ | Set-Content -Encoding utf8 $harnessProject
    Invoke-Dotnet -Arguments @('build', $harnessProject, '-c', 'Release', '-o', (Join-Path $output 'harness'))
    Invoke-Dotnet -Arguments @((Join-Path $output 'harness/NativeContract.dll'), (Join-Path $output '1.0.0'), (Join-Path $output '1.0.1'), (Join-Path $output 'helper/DesktopUpdater.exe'), (Get-Command dotnet).Source)
} finally {
    # All artifacts are fixtures generated in this run, never installed applications.
    if (Test-Path $output) { Remove-Item -Recurse -Force $output }
}
