$ErrorActionPreference = 'Stop'

$manifestUrl = '__TEMPERAI_MANIFEST_URL__'
$targetRid = 'win-x64'
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\TemperAI'
$targetExe = Join-Path $installDir 'temper-ai.exe'
$stateDir = Join-Path $installDir 'state'
$metadataPath = Join-Path $stateDir 'install-metadata.json'

Write-Host ''
Write-Host '========================================' -ForegroundColor Magenta
Write-Host '     TemperAI Community Installer       ' -ForegroundColor Magenta
Write-Host '========================================' -ForegroundColor Magenta
Write-Host ''

Write-Host '[1/5] Resolviendo manifest estable...' -ForegroundColor Yellow
$manifest = Invoke-RestMethod -Uri $manifestUrl -Headers @{ 'User-Agent' = 'TemperAI/1.0' }

if ($null -eq $manifest) {
    throw 'No se pudo resolver el manifest estable.'
}

$cliArtifact = $manifest.cli.platforms | Where-Object { $_.rid -eq $targetRid } | Select-Object -First 1
if ($null -eq $cliArtifact) {
    throw "El manifest no define un artefacto CLI para $targetRid."
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('temper-ai-install-' + [System.Guid]::NewGuid().ToString('N'))
$zipPath = Join-Path $tempRoot 'temper-ai.zip'
$extractPath = Join-Path $tempRoot 'extract'
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

Write-Host '[2/5] Descargando CLI publicado...' -ForegroundColor Yellow
Invoke-WebRequest -Uri $cliArtifact.url -OutFile $zipPath -Headers @{ 'User-Agent' = 'TemperAI/1.0' }

Write-Host '[3/5] Instalando CLI global...' -ForegroundColor Yellow
Expand-Archive -LiteralPath $zipPath -DestinationPath $extractPath -Force
$downloadedExe = Get-ChildItem -LiteralPath $extractPath -Filter 'temper-ai.exe' -Recurse | Select-Object -First 1

if ($null -eq $downloadedExe) {
    throw 'El paquete CLI descargado no contiene temper-ai.exe.'
}

New-Item -ItemType Directory -Path $installDir -Force | Out-Null
Copy-Item -LiteralPath $downloadedExe.FullName -Destination $targetExe -Force

Write-Host '[4/5] Configurando PATH y modo remoto...' -ForegroundColor Yellow
$currentPath = [Environment]::GetEnvironmentVariable('PATH', 'User')
if ($currentPath -notlike "*$installDir*") {
    $newPath = if ([string]::IsNullOrWhiteSpace($currentPath)) {
        $installDir
    }
    else {
        "$currentPath;$installDir"
    }

    [Environment]::SetEnvironmentVariable('PATH', $newPath, 'User')
}

New-Item -ItemType Directory -Path $stateDir -Force | Out-Null
$now = [System.DateTimeOffset]::UtcNow.ToString('o')
$metadata = [ordered]@{
    channel = $manifest.channel
    sourceMode = 'remote'
    manifestUrl = $manifestUrl
    installedCliVersion = $manifest.cli.version
    installedAssetsVersion = $manifest.assets.version
    installedAt = $now
    lastUpdatedAt = $now
}
$metadata | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $metadataPath -Encoding utf8

Write-Host '[5/5] Verificando instalación...' -ForegroundColor Yellow
if (-not (Test-Path -LiteralPath $targetExe)) {
    throw 'La instalación falló porque no se encontró temper-ai.exe en el destino final.'
}

$version = & $targetExe --version 2>$null

Write-Host ''
Write-Host '========================================' -ForegroundColor DarkGray
Write-Host ''
Write-Host '> Instalación exitosa!' -ForegroundColor Green
Write-Host "  Version: $($manifest.version)" -ForegroundColor Green
Write-Host "  Canal:   $($manifest.channel)" -ForegroundColor Green
Write-Host ''
Write-Host 'Ahora podés usar ' -NoNewline
Write-Host 'temper-ai' -ForegroundColor Cyan -NoNewline
Write-Host ' desde cualquier terminal.'
Write-Host ''
Write-Host 'Siguientes pasos:' -ForegroundColor Yellow
Write-Host '  temper-ai' -ForegroundColor Cyan
Write-Host '  temper-ai install' -ForegroundColor Cyan
Write-Host '  temper-ai update' -ForegroundColor Cyan
Write-Host ''
Write-Host '! Importante: reiniciá tu terminal para que los cambios del PATH surtan efecto.' -ForegroundColor Yellow
Write-Host ''
Write-Host '========================================' -ForegroundColor DarkGray
