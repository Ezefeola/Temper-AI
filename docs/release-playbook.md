# TemperAI Release Playbook

## Purpose

This document explains exactly how to publish a new public TemperAI release.

It is the practical operator guide for maintainers.

Use this playbook together with:

- `docs/distribution-operations.md`
- `docs/community-release-model.md`

## Release Model Summary

TemperAI public releases are published from GitHub using:

- a self-contained CLI bundle
- a remote assets bundle
- a mandatory `manifest.json`
- a bootstrap `install.ps1`

Important rules:

- public stable releases are published only from commits already contained in `main`
- pushes to `test` do not publish releases
- pull requests to `main` run validation only
- merging to `main` does not publish automatically
- a release is published only when you push a version tag like `v0.2.0`

## Branch Strategy

Expected branch usage:

- `test`: day-to-day work, validation, pre-release checks
- `main`: stable integration branch for public releases

Recommended flow:

1. Develop in `test`
2. Validate locally
3. Open PR from `test` to `main`
4. Wait for GitHub build/test to pass
5. Merge into `main`
6. Tag the `main` commit you want to publish

## Before You Release

Make sure all of this is true:

1. Your changes are already merged into `main`
2. `Build and Test` is passing
3. The version you want to publish is final
4. The release content is correct:
   - CLI changes complete
   - assets changes complete
   - docs updated if needed

## Local Validation Before Tagging

Run these commands locally from the repository root.

### 1. Pull latest `main`

```bash
git checkout main
git pull
```

### 2. Run tests

```bash
dotnet test TemperAI.slnx -c Release
```

### 3. Build the release bundle locally

This lets you validate the exact release artifacts before publishing.

```powershell
pwsh ./scripts/release/Build-CommunityReleaseBundle.ps1 `
  -Version 0.2.0 `
  -Repository Ezefeola/temper-ai `
  -OutputRoot artifacts/release
```

Expected output:

- `artifacts/release/temper-ai-win-x64.zip`
- `artifacts/release/temper-ai-assets-0.2.0.zip`
- `artifacts/release/manifest.json`
- `artifacts/release/install.ps1`

### 4. Inspect `manifest.json`

Check that it contains:

- correct `version`
- `channel = stable`
- correct CLI asset URL
- correct assets URL
- SHA-256 hashes
- compatibility block with `single-action`

## How To Publish A Release

Once `main` is ready, publish with a version tag.

### Commands

```bash
git checkout main
git pull
git tag v0.2.0
git push origin v0.2.0
```

That is the release trigger.

## What GitHub Actions Does After The Tag

When you push a tag like `v0.2.0`:

1. GitHub Actions starts the `Community Release` workflow
2. The workflow checks that the tagged commit is contained in `origin/main`
3. If the commit is not in `main`, publish fails and no stable release is created
4. If the commit is in `main`, the workflow:
   - builds the CLI
   - creates the CLI zip
   - creates the assets zip
   - generates `manifest.json`
   - publishes the GitHub Release assets

## Expected Release Assets

For version `v0.2.0`, the release should contain:

- `temper-ai-win-x64.zip`
- `temper-ai-assets-0.2.0.zip`
- `manifest.json`
- `install.ps1`

## How To Verify The Release Succeeded

### 1. Check GitHub Actions

Go to the Actions tab and verify that `Community Release` passed.

### 2. Check the GitHub Release page

Verify there is a new release for the tag, for example:

- `TemperAI v0.2.0`

Confirm the assets are attached:

- CLI zip
- assets zip
- `manifest.json`
- `install.ps1`

### 3. Check the generated manifest URLs

Open the published `manifest.json` and confirm the URLs point to the current tag.

Example pattern:

- `https://github.com/Ezefeola/temper-ai/releases/download/v0.2.0/temper-ai-win-x64.zip`
- `https://github.com/Ezefeola/temper-ai/releases/download/v0.2.0/temper-ai-assets-0.2.0.zip`

## How To Test The Installer After Release

The installer is only fully testable after the release assets are public.

### Option A: Test the published installer directly

Download or invoke the published bootstrap installer and run it on a clean machine or a clean local environment.

If using the release asset directly, verify it:

1. resolves the stable manifest
2. downloads the CLI zip
3. installs `temper-ai.exe`
4. writes install metadata
5. adds the install directory to PATH

### Option B: Test the installed CLI manually

After installation:

```powershell
temper-ai --help
temper-ai
temper-ai status
```

Expected results:

- `temper-ai --help` shows the full command set
- `temper-ai` opens the interactive menu in a real terminal
- `temper-ai status` shows install metadata and OpenCode paths

### Option C: Test install and update flows

```powershell
temper-ai install
temper-ai update
```

Verify that:

- install uses remote assets by default
- update behaves as one logical action for CLI + assets
- no local repo checkout is required for community behavior

## How To Test Local Development Mode

From the repository root:

```powershell
temper-ai install --source local
temper-ai update --source local
```

Verify that:

- assets are sourced from the local `assets/` directory
- this does not require a public release
- your contributor workflow still works before publishing

## What Happens If You Tag From `test`

Example:

```bash
git checkout test
git tag v0.2.0
git push origin v0.2.0
```

What happens:

1. The workflow starts because the tag matches `v*`
2. The guard checks whether the commit is contained in `main`
3. The check fails
4. The stable release is not published

This is expected behavior.

## Release Checklist

Use this checklist every time.

### Code readiness

- [ ] Changes are merged into `main`
- [ ] Tests pass locally or in CI
- [ ] Docs are updated if needed

### Artifact readiness

- [ ] Local release bundle builds successfully
- [ ] `manifest.json` has the correct version and URLs
- [ ] Assets zip contains the expected runtime assets

### Publish

- [ ] You are on `main`
- [ ] `git pull` completed
- [ ] Version tag created as `vX.Y.Z`
- [ ] Tag pushed to origin

### Post-release verification

- [ ] GitHub Actions workflow passed
- [ ] GitHub Release exists
- [ ] Release assets are attached
- [ ] Installer works
- [ ] `temper-ai install` works
- [ ] `temper-ai update` works

## Example Full Release Session

```bash
git checkout main
git pull
dotnet test TemperAI.slnx -c Release
pwsh ./scripts/release/Build-CommunityReleaseBundle.ps1 -Version 0.2.0 -Repository Ezefeola/temper-ai -OutputRoot artifacts/release
git tag v0.2.0
git push origin v0.2.0
```

Then verify in GitHub:

1. Actions passed
2. Release created
3. Assets uploaded
4. Installer works

## Failure Cases

### The workflow ran but no release was published

Likely causes:

- the tag points to a commit not contained in `main`
- build or test failed
- release bundle generation failed

### The installer fails after release

Likely causes:

- incorrect manifest URL
- missing release asset
- wrong asset filename in manifest
- CLI zip does not contain `temper-ai.exe`

### `temper-ai update` does not resolve the expected version

Likely causes:

- manifest version mismatch
- incorrect asset URL in manifest
- stale local install metadata

## Operator Rule

If the release is public and stable, always publish it from a tagged commit already contained in `main`.

Do not treat a merge to `main` as the release trigger.

The release trigger is the version tag.
