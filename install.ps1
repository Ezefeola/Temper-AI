$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "     TemperAI CLI Installer             " -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""

$installDir = Join-Path $env:LOCALAPPDATA "Programs\TemperAI"
$targetExe = Join-Path $installDir "temper-ai.exe"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "[1/3] Publicando TemperAI CLI..." -ForegroundColor Yellow
dotnet publish "$scriptDir\src\TemperAI.Cli\TemperAI.Cli.csproj" -c Release -o $installDir --nologo -v q

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "X Error al publicar. Asegurate de tener .NET 10 SDK instalado." -ForegroundColor Red
    Write-Host ""
    exit 1
}

Write-Host "  > Publicado en: $installDir" -ForegroundColor Green

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

Write-Host ""
Write-Host "[3/3] Verificando instalacion..." -ForegroundColor Yellow

if (Test-Path $targetExe) {
    Write-Host "  > Ejecutable encontrado: $targetExe" -ForegroundColor Green
} else {
    Write-Host "  X Ejecutable no encontrado" -ForegroundColor Red
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
Write-Host "! Importante: Reinicia tu terminal para que los cambios en el PATH surtan efecto." -ForegroundColor Yellow
Write-Host ""
Write-Host "========================================" -ForegroundColor DarkGray
Write-Host ""
