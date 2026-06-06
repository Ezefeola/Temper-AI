[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$Repository,

    [string]$Channel = 'stable',
    [string]$RuntimeIdentifier = 'win-x64',
    [string]$OutputRoot = 'artifacts/release'
)

$ErrorActionPreference = 'Stop'

$normalizedVersion = $Version.Trim()
if ($normalizedVersion.StartsWith('v', [System.StringComparison]::OrdinalIgnoreCase)) {
    $normalizedVersion = $normalizedVersion.Substring(1)
}

if ($normalizedVersion -notmatch '^\d+\.\d+\.\d+$') {
    throw "Stable community releases require a semantic version tag in the form vX.Y.Z or X.Y.Z. Received: '$Version'."
}

if ($Channel -ne 'stable') {
    throw "Only the stable public release channel is currently supported. Received: '$Channel'."
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$cliProject = Join-Path $repoRoot 'src\TemperAI.Cli\TemperAI.Cli.csproj'
$assetsRoot = Join-Path $repoRoot 'assets'
$bootstrapTemplate = Join-Path $PSScriptRoot 'install-template.ps1'

foreach ($requiredPath in @($cliProject, $assetsRoot, $bootstrapTemplate)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required release input not found: $requiredPath"
    }
}

$releaseTag = "v$normalizedVersion"
$releaseBaseUrl = "https://github.com/$Repository/releases/download/$releaseTag"
$cliAssetName = 'temper-ai-win-x64.zip'
$assetsAssetName = "temper-ai-assets-$normalizedVersion.zip"

$resolvedOutputRoot = Join-Path $repoRoot $OutputRoot
$publishDir = Join-Path $resolvedOutputRoot 'publish'
$workingDir = Join-Path $resolvedOutputRoot 'working'
$cliPublishDir = Join-Path $publishDir 'cli'

if (Test-Path -LiteralPath $resolvedOutputRoot) {
    Remove-Item -LiteralPath $resolvedOutputRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $cliPublishDir -Force | Out-Null
New-Item -ItemType Directory -Path $workingDir -Force | Out-Null

& dotnet publish $cliProject `
    --configuration Release `
    --runtime $RuntimeIdentifier `
    --self-contained true `
    --output $cliPublishDir `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=false `
    -p:Version=$normalizedVersion `
    -p:AssemblyVersion=$normalizedVersion `
    -p:FileVersion=$normalizedVersion

if ($LASTEXITCODE -ne 0) {
    throw 'dotnet publish failed while building the community CLI artifact.'
}

$cliExecutable = Join-Path $cliPublishDir 'temper-ai.exe'
if (-not (Test-Path -LiteralPath $cliExecutable)) {
    throw "Published CLI executable not found: $cliExecutable"
}

$cliZipPath = Join-Path $resolvedOutputRoot $cliAssetName
$assetsZipPath = Join-Path $resolvedOutputRoot $assetsAssetName
$manifestPath = Join-Path $resolvedOutputRoot 'manifest.json'
$bootstrapOutputPath = Join-Path $resolvedOutputRoot 'install.ps1'

Compress-Archive -Path $cliExecutable -DestinationPath $cliZipPath -CompressionLevel Optimal
Compress-Archive -Path $assetsRoot -DestinationPath $assetsZipPath -CompressionLevel Optimal
Copy-Item -LiteralPath $bootstrapTemplate -Destination $bootstrapOutputPath -Force

$cliHash = (Get-FileHash -LiteralPath $cliZipPath -Algorithm SHA256).Hash.ToLowerInvariant()
$assetsHash = (Get-FileHash -LiteralPath $assetsZipPath -Algorithm SHA256).Hash.ToLowerInvariant()

$manifest = [ordered]@{
    product = 'temper-ai'
    version = $normalizedVersion
    channel = $Channel
    publishedAt = (Get-Date).ToUniversalTime().ToString('o')
    cli = [ordered]@{
        version = $normalizedVersion
        platforms = @(
            [ordered]@{
                rid = $RuntimeIdentifier
                url = "$releaseBaseUrl/$cliAssetName"
                sha256 = $cliHash
            }
        )
    }
    assets = [ordered]@{
        version = $normalizedVersion
        url = "$releaseBaseUrl/$assetsAssetName"
        sha256 = $assetsHash
    }
    compatibility = [ordered]@{
        cliVersion = $normalizedVersion
        assetsVersion = $normalizedVersion
        updateMode = 'single-action'
    }
}

$manifest | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $manifestPath -Encoding utf8

Write-Host "Created community release bundles:"
Write-Host " - $cliZipPath"
Write-Host " - $assetsZipPath"
Write-Host " - $manifestPath"
Write-Host " - $bootstrapOutputPath"
