$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "     TemperAI Installer                 " -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""

$installDir  = Join-Path $env:LOCALAPPDATA "Programs\TemperAI"
$targetExe   = Join-Path $installDir "temper-ai.exe"
$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceExe   = Join-Path $scriptDir "temper-ai.exe"

# ---------------------------------------------------------------------------
# [1/3] Copy the self-contained binary
# ---------------------------------------------------------------------------
Write-Host "[1/3] Instalando TemperAI CLI..." -ForegroundColor Yellow

if (-not (Test-Path $sourceExe)) {
    Write-Host ""
    Write-Host "X No se encontro temper-ai.exe junto al installer." -ForegroundColor Red
    Write-Host "  Asegurate de descargar el paquete completo desde:" -ForegroundColor Red
    Write-Host "  https://github.com/ezefeDev/temper-ai/releases" -ForegroundColor Cyan
    Write-Host ""
    exit 1
}

if (-not (Test-Path $installDir)) {
    New-Item -ItemType Directory -Path $installDir -Force | Out-Null
}

Copy-Item -Path $sourceExe -Destination $targetExe -Force
Write-Host "  > Instalado en: $targetExe" -ForegroundColor Green

# ---------------------------------------------------------------------------
# [2/3] Add to user PATH
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "[2/3] Agregando al PATH del usuario..." -ForegroundColor Yellow

$currentPath = [Environment]::GetEnvironmentVariable("PATH", "User")

if ($currentPath -notlike "*$installDir*") {
    $newPath = if ([string]::IsNullOrEmpty($currentPath)) {
        $installDir
    } else {
        "$currentPath;$installDir"
    }

    [Environment]::SetEnvironmentVariable("PATH", $newPath, "User")
    Write-Host "  > PATH actualizado" -ForegroundColor Green
} else {
    Write-Host "  > Ya estaba en el PATH" -ForegroundColor Green
}

# ---------------------------------------------------------------------------
# [3/3] Verify
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "[3/3] Verificando instalacion..." -ForegroundColor Yellow

if (Test-Path $targetExe) {
    $version = & $targetExe --version 2>$null
    Write-Host "  > Ejecutable verificado: $targetExe" -ForegroundColor Green
} else {
    Write-Host "  X Ejecutable no encontrado." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor DarkGray
Write-Host ""
Write-Host "> Instalacion exitosa!" -ForegroundColor Green
Write-Host ""
Write-Host "Ahora podes usar " -NoNewline
Write-Host "temper-ai" -ForegroundColor Cyan -NoNewline
Write-Host " desde cualquier terminal."
Write-Host ""
Write-Host "Primeros pasos:" -ForegroundColor Yellow
Write-Host "  temper-ai install     " -NoNewline -ForegroundColor Cyan
Write-Host "Instala skills y agentes en OpenCode"
Write-Host "  temper-ai status      " -NoNewline -ForegroundColor Cyan
Write-Host "Verifica el estado de la instalacion"
Write-Host "  temper-ai --help      " -NoNewline -ForegroundColor Cyan
Write-Host "Ver todos los comandos disponibles"
Write-Host ""
Write-Host "! Importante: Reinicia tu terminal para que los cambios en el PATH surtan efecto." -ForegroundColor Yellow
Write-Host ""
Write-Host "========================================" -ForegroundColor DarkGray
Write-Host ""
