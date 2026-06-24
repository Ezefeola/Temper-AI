[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [switch]$Install
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "     TemperAI Local Installer            " -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""

$repoRoot   = Split-Path -Parent $MyInvocation.MyCommand.Path
$cliProject = Join-Path $repoRoot "src\TemperAI.Cli\TemperAI.Cli.csproj"
$installDir = Join-Path $env:LOCALAPPDATA "Programs\TemperAI"
$targetExe  = Join-Path $installDir "temper-ai.exe"
$publishDir = Join-Path $repoRoot "artifacts\local-cli"

if (-not (Test-Path $cliProject)) {
    Write-Host "X No se encontro el proyecto CLI: $cliProject" -ForegroundColor Red
    Write-Host "  Ejecuta este script desde la raiz del repo temper-ai." -ForegroundColor Red
    exit 1
}

# ---------------------------------------------------------------------------
# [1/4] Build (publish self-contained single-file exe from source)
# ---------------------------------------------------------------------------
Write-Host "[1/4] Compilando temper-ai desde el codigo..." -ForegroundColor Yellow

if (Test-Path $publishDir) {
    Remove-Item -Path $publishDir -Recurse -Force
}

& dotnet publish $cliProject -c $Configuration -o $publishDir | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host "X Fallo dotnet publish." -ForegroundColor Red
    exit 1
}

$sourceExe = Join-Path $publishDir "temper-ai.exe"

if (-not (Test-Path $sourceExe)) {
    Write-Host "X No se genero temper-ai.exe en $publishDir" -ForegroundColor Red
    exit 1
}

Write-Host "  > Compilado: $sourceExe" -ForegroundColor Green

# ---------------------------------------------------------------------------
# [2/4] Install the binary into the user programs folder
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "[2/4] Instalando el CLI..." -ForegroundColor Yellow

if (-not (Test-Path $installDir)) {
    New-Item -ItemType Directory -Path $installDir -Force | Out-Null
}

Copy-Item -Path $sourceExe -Destination $targetExe -Force
Write-Host "  > Instalado en: $targetExe" -ForegroundColor Green

# ---------------------------------------------------------------------------
# [3/4] Add to user PATH
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "[3/4] Agregando al PATH del usuario..." -ForegroundColor Yellow

$currentPath = [Environment]::GetEnvironmentVariable("PATH", "User")

if ($currentPath -notlike "*$installDir*") {
    $newPath = if ([string]::IsNullOrEmpty($currentPath)) {
        $installDir
    } else {
        "$currentPath;$installDir"
    }

    [Environment]::SetEnvironmentVariable("PATH", $newPath, "User")
    # Make it usable in the current session too.
    $env:PATH = "$env:PATH;$installDir"
    Write-Host "  > PATH actualizado" -ForegroundColor Green
} else {
    Write-Host "  > Ya estaba en el PATH" -ForegroundColor Green
}

# ---------------------------------------------------------------------------
# [4/4] Verify
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "[4/4] Verificando instalacion..." -ForegroundColor Yellow

$version = & $targetExe --version 2>$null
Write-Host "  > Ejecutable verificado: $targetExe" -ForegroundColor Green

# ---------------------------------------------------------------------------
# Optional: run a local asset install right away
# ---------------------------------------------------------------------------
if ($Install) {
    Write-Host ""
    Write-Host "Instalando assets locales en todos los proveedores..." -ForegroundColor Yellow
    Push-Location $repoRoot
    try {
        & $targetExe --local install -a all --neuralcore false
    } finally {
        Pop-Location
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor DarkGray
Write-Host ""
Write-Host "> Instalacion local exitosa!" -ForegroundColor Green
Write-Host ""
Write-Host "Importante: corre 'temper-ai --local ...' parado en el repo (usa los assets locales)." -ForegroundColor Yellow
Write-Host ""
Write-Host "Primeros pasos:" -ForegroundColor Yellow
Write-Host "  temper-ai --local                  " -NoNewline -ForegroundColor Cyan
Write-Host "Menu interactivo en modo local"
Write-Host "  temper-ai --local install -a all   " -NoNewline -ForegroundColor Cyan
Write-Host "Instala assets locales (OpenCode + Claude)"
Write-Host "  temper-ai --local update  -a all   " -NoNewline -ForegroundColor Cyan
Write-Host "Re-aplica assets tras editarlos (overwrite)"
Write-Host ""
Write-Host "! Reinicia la terminal si 'temper-ai' no se reconoce todavia (PATH)." -ForegroundColor Yellow
Write-Host ""
Write-Host "========================================" -ForegroundColor DarkGray
Write-Host ""
