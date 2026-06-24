# Proceso de Release — TemperAI

Esta guía describe cómo publicar una release pública estable ("Community Release")
de TemperAI. El proceso es totalmente automatizado por GitHub Actions: alcanza con
crear un tag de Git y empujarlo al repositorio.

## Resumen

Una release consiste en:

1. Un commit que ya está en la rama `main`.
2. Un tag de Git con formato `vX.Y.Z` (versión semántica) apuntando a ese commit.
3. El push de ese tag a `origin`, que dispara el workflow **Community Release**
   (`.github/workflows/community-release.yml`).

El workflow se ejecuta en dos etapas:

- **Job `build` (Build and Test)** — corre en `ubuntu-latest`. Restaura, compila en
  `Release` y ejecuta los tests de la solución `TemperAI.slnx`.
- **Job `publish` (Publish stable community release)** — corre en `windows-latest`
  solo cuando el ref empujado es un tag `v*`. Depende de que `build` haya pasado.
  Verifica que el commit esté en `main`, construye los bundles de release con un
  script de PowerShell y publica una GitHub Release con los assets.

No hay que ejecutar nada manualmente más allá de crear y empujar el tag.

## Prerrequisitos

- **El commit del tag debe estar ya en `origin/main`.** El job `publish` ejecuta el
  paso *"Ensure tagged commit belongs to main"*, que falla con error si el commit del
  tag no está contenido en `origin/main`. Las releases estables solo se permiten desde
  commits que ya están en `main`.
- **El job `build` (compilación + tests) debe pasar.** El job `publish` depende de
  `build` (`needs: build`); si la compilación o los tests fallan, no se publica nada.
- El tag debe tener formato de versión semántica `vX.Y.Z` (o `X.Y.Z`). El script de
  build valida el patrón `^\d+\.\d+\.\d+$` y aborta si no se cumple.

## Procedimiento paso a paso (ejemplo: versión 1.0.0)

1. Asegurate de estar en `main` y de tener la última versión:

   ```bash
   git checkout main
   git pull origin main
   ```

2. (Opcional pero recomendado) Verificá que el commit que vas a taggear sea el que
   querés publicar:

   ```bash
   git log -1 --oneline
   ```

3. Creá el tag. Se recomienda un tag anotado:

   ```bash
   git tag -a v1.0.0 -m "TemperAI 1.0.0"
   ```

   (Alternativa con tag liviano: `git tag v1.0.0`.)

4. Empujá el tag a `origin`:

   ```bash
   git push origin v1.0.0
   ```

### Qué pasa automáticamente después del push

1. Se dispara el workflow **Community Release** por el push del tag `v1.0.0`.
2. Corre el job **build**: restore → `dotnet build` (Release) → `dotnet test`. Si algo
   falla, el proceso se detiene y no hay publicación.
3. Si `build` pasa, corre el job **publish** en `windows-latest`:
   - Verifica que el commit del tag esté en `origin/main` (si no, falla).
   - Ejecuta `scripts/release/Build-CommunityReleaseBundle.ps1` con
     `-Version "v1.0.0"`, generando los bundles de release.
   - Sube los bundles como artifact de la corrida (`community-release-bundles`).
   - Publica una **GitHub Release** llamada `TemperAI v1.0.0`, marcada como `latest`
     y no-prerelease, adjuntando los assets.

## Versionado: de dónde sale el número de versión

**La versión se deriva exclusivamente del tag de Git** (`github.ref_name`). No hace
falta bumpear ningún archivo antes de taggear.

- El workflow pasa el tag al script:
  `Build-CommunityReleaseBundle.ps1 -Version "${{ github.ref_name }}"`.
- El script normaliza la versión (saca el prefijo `v`), valida que sea `X.Y.Z` y la
  inyecta en el binario publicado mediante:
  `-p:Version=... -p:AssemblyVersion=... -p:FileVersion=...`.
- Ese mismo número se escribe en el `manifest.json` (campos `version`, `cli.version`,
  `assets.version`, `compatibility.*`).

> Nota sobre `src/TemperAI.Cli/TemperAI.Cli.csproj`: ese proyecto declara
> `<Version>0.1.0</Version>`, pero ese valor **no** es la fuente de verdad para las
> releases — `dotnet publish` lo sobreescribe con la versión del tag vía
> `-p:Version=...`. Por lo tanto, **no es necesario bumpear el `.csproj`** para sacar
> una release; basta con crear y empujar el tag.

Para publicar **1.0.0**, alcanza con crear el tag `v1.0.0` (commit en `main`) y
empujarlo: el binario, el manifest y la GitHub Release quedarán versionados como
`1.0.0` automáticamente.

## Artefactos publicados

El script `Build-CommunityReleaseBundle.ps1` produce en `artifacts/release/` y la
GitHub Release adjunta los siguientes assets:

| Asset | Descripción |
|-------|-------------|
| `temper-ai-win-x64.zip` | CLI `temper-ai.exe` publicado como ejecutable único, self-contained, `win-x64`. |
| `temper-ai-assets-<version>.zip` | Bundle de la carpeta `assets/` (ej.: `temper-ai-assets-1.0.0.zip`). |
| `manifest.json` | Manifest estable con versión, canal, URLs de descarga y hashes SHA-256 de los bundles. |
| `install.ps1` | Instalador comunitario generado desde `scripts/release/install-template.ps1`, con la URL del manifest estable embebida. |

Detalles relevantes:

- **Runner de publicación:** `windows-latest` (la compilación/tests corren en
  `ubuntu-latest`).
- El `manifest.json` apunta a las URLs de descarga de la propia release
  (`releases/download/v<version>/...`) e incluye los `sha256` de los dos `.zip`.
- El `install.ps1` resuelve siempre el manifest "latest"
  (`releases/latest/download/manifest.json`), descarga el CLI, lo instala en
  `%LOCALAPPDATA%\Programs\TemperAI` y agrega esa ruta al `PATH` del usuario.
- La publicación usa `fail_on_unmatched_files: true`: si algún asset esperado no se
  generó, el paso de publicación falla.

## Troubleshooting

### El workflow falla: "tag ... is not contained in origin/main"

Causa: el commit al que apunta el tag no está en `origin/main`. Las releases estables
solo se permiten desde commits ya mergeados a `main`.

Solución: borrar el tag, llevar el commit a `main` y volver a taggear sobre el commit
correcto.

```bash
# Borrar el tag local y remoto
git tag -d v1.0.0
git push origin :refs/tags/v1.0.0

# Asegurar que el commit esté en main (mergear el PR / la rama correspondiente)
git checkout main
git pull origin main

# Re-crear el tag sobre el commit ya en main y re-empujar
git tag -a v1.0.0 -m "TemperAI 1.0.0"
git push origin v1.0.0
```

### El workflow falla en el job build (compilación o tests)

El job `publish` no se ejecuta si `build` falla. Corregí el error de compilación o el
test que falla en `main`, mergeá el fix y volvé a taggear (mismo procedimiento de
borrado/re-tag de arriba).

### El tag no respeta el formato semántico

El script aborta si la versión no coincide con `X.Y.Z`. Usá un tag como `v1.0.0`
(no `v1.0`, `v1`, ni sufijos). Borrá el tag inválido y creá uno con formato correcto.
