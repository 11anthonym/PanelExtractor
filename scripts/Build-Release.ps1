[CmdletBinding()]
param(
    [string]$OutputDirectory = "artifacts\release",
    [string]$DotNetPath = "dotnet"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot "artifacts"))
$releaseRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
$artifactsPrefix = $artifactsRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) +
    [IO.Path]::DirectorySeparatorChar

if ($releaseRoot -eq $artifactsRoot -or
    !$releaseRoot.StartsWith($artifactsPrefix, [StringComparison]::OrdinalIgnoreCase))
{
    throw "Release output must be a child of $artifactsRoot."
}

$dotnet = (Get-Command $DotNetPath -ErrorAction Stop).Source
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:DOTNET_NOLOGO = "1"

$installedSdks = @(& $dotnet --list-sdks)
if ($LASTEXITCODE -ne 0 -or !($installedSdks -match '^10\.'))
{
    throw "Release generation requires the .NET 10 SDK."
}

$installedRuntimes = @(& $dotnet --list-runtimes)
if ($LASTEXITCODE -ne 0 -or !($installedRuntimes -match '^Microsoft\.NETCore\.App 8\.'))
{
    throw "Release generation also requires the .NET 8 runtime for the pinned SBOM tool."
}

[xml]$project = Get-Content -LiteralPath (Join-Path $repoRoot "PanelExtractor.csproj") -Raw
$versionNode = $project.SelectSingleNode("/Project/PropertyGroup/Version")
if ($null -eq $versionNode)
{
    throw "PanelExtractor.csproj does not define a version."
}
$version = $versionNode.InnerText

if ($env:GITHUB_REF_TYPE -eq "tag" -and $env:GITHUB_REF_NAME -ne "v$version")
{
    throw "Tag $($env:GITHUB_REF_NAME) does not match project version $version."
}

function Invoke-DotNet
{
    param([Parameter(Mandatory)][string[]]$Arguments)

    & $dotnet @Arguments
    if ($LASTEXITCODE -ne 0)
    {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

Push-Location $repoRoot
try
{
    if (Test-Path -LiteralPath $releaseRoot)
    {
        Remove-Item -LiteralPath $releaseRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Path $releaseRoot | Out-Null

    Invoke-DotNet -Arguments @("tool", "restore")
    Invoke-DotNet -Arguments @("restore", "PanelExtractor.sln")
    Invoke-DotNet -Arguments @(
        "build", "PanelExtractor.sln",
        "--configuration", "Release", "--no-restore", "-warnaserror")
    Invoke-DotNet -Arguments @(
        "test", "PanelExtractor.sln",
        "--configuration", "Release", "--no-build",
        "--filter", "TestCategory!=Integration")
    Invoke-DotNet -Arguments @(
        "publish", "PanelExtractor.csproj",
        "--configuration", "Release",
        "--runtime", "win-x64",
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
        "-p:EnableCompressionInSingleFile=true",
        "-p:DebugType=None",
        "-p:DebugSymbols=false",
        "-o", $releaseRoot)

    $executables = @(Get-ChildItem -LiteralPath $releaseRoot -Filter "*.exe" -File)
    if ($executables.Count -ne 1)
    {
        throw "Expected one published executable; found $($executables.Count)."
    }

    if (@(Get-ChildItem -LiteralPath $releaseRoot -Filter "*.dll" -File).Count -ne 0)
    {
        throw "Expected a self-contained single-file executable, but DLLs were published beside it."
    }

    foreach ($notice in @("LICENSE", "THIRD-PARTY-NOTICES.md"))
    {
        if (!(Test-Path -LiteralPath (Join-Path $releaseRoot $notice) -PathType Leaf))
        {
            throw "Published output is missing $notice."
        }
    }

    $publishedFiles = @(Get-ChildItem -LiteralPath $releaseRoot -File | Sort-Object Name)
    if ($publishedFiles.Count -eq 0 -or $publishedFiles.Name -contains "PanelExtractor.pdb")
    {
        throw "Published output is empty or contains debug symbols."
    }

    $componentRoot = Join-Path $artifactsRoot (
        "sbom-components-" + [Guid]::NewGuid().ToString("N"))
    $componentObjectRoot = Join-Path $componentRoot "obj"
    New-Item -ItemType Directory -Path $componentObjectRoot | Out-Null

    Copy-Item -LiteralPath "PanelExtractor.csproj" -Destination $componentRoot
    foreach ($componentFile in @(
        "project.assets.json",
        "PanelExtractor.csproj.nuget.dgspec.json"))
    {
        $componentPath = Join-Path "obj" $componentFile
        Copy-Item -LiteralPath $componentPath -Destination $componentObjectRoot
    }

    $validationPath = Join-Path $artifactsRoot (
        "sbom-validation-" + [Guid]::NewGuid().ToString("N") + ".json")
    try
    {
        Invoke-DotNet -Arguments @(
            "tool", "run", "sbom-tool", "--", "Generate",
            "-b", $releaseRoot,
            "-bc", $componentRoot,
            "-pn", "PanelExtractor",
            "-pv", $version,
            "-ps", "Anthony Moretti",
            "-nsb", "https://github.com/11anthonym/PanelExtractor",
            "-D", "true",
            "-pm", "true",
            "-mi", "SPDX:2.2",
            "-V", "Warning")

        Invoke-DotNet -Arguments @(
            "tool", "run", "sbom-tool", "--", "Validate",
            "-b", $releaseRoot,
            "-o", $validationPath,
            "-n",
            "-mi", "SPDX:2.2",
            "-V", "Warning")
    }
    finally
    {
        Remove-Item -LiteralPath $validationPath -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $componentRoot -Recurse -Force -ErrorAction SilentlyContinue
    }

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $packageName = "PanelExtractor-$version-windows"
    $packageArchivePath = Join-Path $releaseRoot "$packageName.zip"
    $packageFiles = @($publishedFiles | ForEach-Object {
        [PSCustomObject]@{ Source = $_.FullName; Path = $_.Name }
    })

    $manifestRoot = Join-Path $releaseRoot "_manifest"
    $manifestFiles = @(Get-ChildItem -LiteralPath $manifestRoot -Recurse -File)
    if ($manifestFiles.Count -eq 0)
    {
        throw "The generated SBOM manifest is missing."
    }

    foreach ($manifestFile in $manifestFiles)
    {
        $relativePath = $manifestFile.FullName.Substring($releaseRoot.Length)
        $packageFiles += [PSCustomObject]@{
            Source = $manifestFile.FullName
            Path = $relativePath.TrimStart([IO.Path]::DirectorySeparatorChar)
        }
    }

    $archive = [IO.Compression.ZipFile]::Open(
        $packageArchivePath,
        [IO.Compression.ZipArchiveMode]::Create)
    try
    {
        foreach ($packageFile in $packageFiles)
        {
            $entryPath = "$packageName/" +
                $packageFile.Path.Replace([IO.Path]::DirectorySeparatorChar, "/")
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $archive,
                $packageFile.Source,
                $entryPath,
                [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally
    {
        $archive.Dispose()
    }

    $checksumPath = Join-Path $releaseRoot "SHA256SUMS.txt"
    $packageHash = Get-FileHash -LiteralPath $packageArchivePath -Algorithm SHA256
    $packageHash = $packageHash.Hash.ToLowerInvariant()
    [IO.File]::WriteAllLines(
        $checksumPath,
        @("$packageHash  $($packageArchivePath | Split-Path -Leaf)"),
        [Text.UTF8Encoding]::new($false))

    Write-Host "Release created at $releaseRoot"
}
finally
{
    Pop-Location
}
