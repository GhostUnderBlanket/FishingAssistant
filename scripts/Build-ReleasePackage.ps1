[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release",

    [string] $OutputDirectory = "",

    [switch] $NoBuild
)

$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$projectDirectory = Join-Path $repositoryRoot "FishingAssistant"
$projectPath = Join-Path $projectDirectory "FishingAssistant.csproj"
$sharedPropsPath = Join-Path $repositoryRoot "Directory.Build.props"
$manifestPath = Join-Path $projectDirectory "manifest.json"
$translationsDirectory = Join-Path $projectDirectory "i18n"

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot "artifacts"
}
else {
    $OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
}

[xml] $projectXml = Get-Content -Raw -LiteralPath $projectPath
[xml] $sharedPropsXml = Get-Content -Raw -LiteralPath $sharedPropsPath

$versionProperties = @($projectXml.Project.PropertyGroup) |
    Where-Object { -not [string]::IsNullOrWhiteSpace([string] $_.VersionPrefix) } |
    Select-Object -First 1

if ($null -eq $versionProperties) {
    throw "The project does not define VersionPrefix."
}

$versionPrefix = [string] $versionProperties.VersionPrefix
$versionSuffix = [string] $versionProperties.VersionSuffix
$version = if ([string]::IsNullOrWhiteSpace($versionSuffix)) {
    $versionPrefix
}
else {
    "$versionPrefix-$versionSuffix"
}

$targetFrameworkProperties = @($sharedPropsXml.Project.PropertyGroup) |
    Where-Object { -not [string]::IsNullOrWhiteSpace([string] $_.TargetFramework) } |
    Select-Object -First 1

if ($null -eq $targetFrameworkProperties) {
    throw "Directory.Build.props does not define TargetFramework."
}

$targetFramework = [string] $targetFrameworkProperties.TargetFramework
$assemblyPath = Join-Path $projectDirectory "bin\$Configuration\$targetFramework\FishingAssistant.dll"

if (-not $NoBuild) {
    & dotnet build $projectPath --configuration $Configuration -p:EnableModZip=false
    if ($LASTEXITCODE -ne 0) {
        throw "The mod build failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) {
    throw "The compiled mod DLL was not found at '$assemblyPath'."
}

$manifestText = [System.IO.File]::ReadAllText($manifestPath)
if (-not $manifestText.Contains("%ProjectVersion%")) {
    throw "The source manifest does not contain the expected %ProjectVersion% token."
}

$resolvedManifestText = $manifestText.Replace("%ProjectVersion%", $version)
$resolvedManifest = $resolvedManifestText | ConvertFrom-Json
if ([string] $resolvedManifest.Version -ne $version) {
    throw "The resolved manifest version does not match project version '$version'."
}

$utf8WithoutBom = [System.Text.UTF8Encoding]::new($false)
$packageEntries = @(
    [PSCustomObject]@{
        Name = "FishingAssistant/FishingAssistant.dll"
        Bytes = [System.IO.File]::ReadAllBytes($assemblyPath)
    }
    [PSCustomObject]@{
        Name = "FishingAssistant/manifest.json"
        Bytes = $utf8WithoutBom.GetBytes($resolvedManifestText)
    }
)

$translationEntries = Get-ChildItem -LiteralPath $translationsDirectory -Filter "*.json" -File |
    Sort-Object Name |
    ForEach-Object {
        [PSCustomObject]@{
            Name = "FishingAssistant/i18n/$($_.Name)"
            Bytes = [System.IO.File]::ReadAllBytes($_.FullName)
        }
    }

$packageEntries = @($packageEntries + $translationEntries) | Sort-Object Name
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$archivePath = Join-Path $OutputDirectory "FishingAssistant $version.zip"
if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}

$fixedTimestamp = [System.DateTimeOffset]::new(
    1980,
    1,
    1,
    0,
    0,
    0,
    [System.TimeSpan]::Zero
)

$archiveStream = [System.IO.File]::Open(
    $archivePath,
    [System.IO.FileMode]::CreateNew,
    [System.IO.FileAccess]::ReadWrite,
    [System.IO.FileShare]::None
)

try {
    $archive = [System.IO.Compression.ZipArchive]::new(
        $archiveStream,
        [System.IO.Compression.ZipArchiveMode]::Create,
        $false
    )

    try {
        foreach ($packageEntry in $packageEntries) {
            $entry = $archive.CreateEntry(
                $packageEntry.Name,
                [System.IO.Compression.CompressionLevel]::Optimal
            )
            $entry.LastWriteTime = $fixedTimestamp
            $entry.ExternalAttributes = 0

            $entryStream = $entry.Open()
            try {
                $entryStream.Write($packageEntry.Bytes, 0, $packageEntry.Bytes.Length)
            }
            finally {
                $entryStream.Dispose()
            }
        }
    }
    finally {
        $archive.Dispose()
    }
}
finally {
    $archiveStream.Dispose()
}

$verificationStream = [System.IO.File]::OpenRead($archivePath)
try {
    $verificationArchive = [System.IO.Compression.ZipArchive]::new(
        $verificationStream,
        [System.IO.Compression.ZipArchiveMode]::Read,
        $false
    )

    try {
        $expectedNames = @($packageEntries.Name | Sort-Object)
        $actualNames = @($verificationArchive.Entries.FullName | Sort-Object)
        $entryDifference = Compare-Object -ReferenceObject $expectedNames -DifferenceObject $actualNames
        if ($null -ne $entryDifference) {
            throw "The release archive contains an unexpected file set."
        }

        $manifestEntry = $verificationArchive.GetEntry("FishingAssistant/manifest.json")
        if ($null -eq $manifestEntry) {
            throw "The release archive does not contain its manifest."
        }

        $manifestStream = $manifestEntry.Open()
        try {
            $manifestReader = [System.IO.StreamReader]::new(
                $manifestStream,
                [System.Text.Encoding]::UTF8,
                $true,
                1024,
                $true
            )
            try {
                $packagedManifestText = $manifestReader.ReadToEnd()
            }
            finally {
                $manifestReader.Dispose()
            }
        }
        finally {
            $manifestStream.Dispose()
        }

        $packagedManifest = $packagedManifestText | ConvertFrom-Json
        if ([string] $packagedManifest.Version -ne $version) {
            throw "The packaged manifest version does not match '$version'."
        }
        if ($packagedManifestText.Contains("%ProjectVersion%")) {
            throw "The packaged manifest still contains the project-version token."
        }
    }
    finally {
        $verificationArchive.Dispose()
    }
}
finally {
    $verificationStream.Dispose()
}

$archiveHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
[PSCustomObject]@{
    Path = $archivePath
    Version = $version
    Entries = $packageEntries.Count
    Sha256 = $archiveHash
}
