---
name: ci-cd
description: >
  CI/CD strategy standards for TemperAI projects. Covers deployment strategies,
  environment management, versioning, release processes, and pipeline design
  principles. Use when planning or implementing deployment and release workflows.
---

# CI/CD Strategy — TemperAI Standards

## Environments

- **Development** — local development, `dotnet run`, SQLite or local database.
- **Staging** — mirrors production, runs on `develop` branch pushes.
- **Production** — runs on `main` branch merges, requires manual approval.

## Versioning

- Use semantic versioning — `MAJOR.MINOR.PATCH`.
- Initial release is `0.1.0`.
- `PATCH` — bug fixes, no new features.
- `MINOR` — new features, backward compatible.
- `MAJOR` — breaking changes.

## Release process

1. Merge feature branch into `develop`.
2. CI runs build and tests on `develop`.
3. When ready for release, create a PR from `develop` to `main`.
4. CI runs full pipeline on `main` PR.
5. After merge, CI publishes Docker image with `latest` and commit SHA tags.
6. Deploy to staging automatically.
7. Deploy to production with manual approval.

## Pipeline design principles

- **Fast feedback** — build and test should complete in under 5 minutes.
- **Reproducible** — every build is deterministic from source code.
- **Secure** — no secrets in logs, no hardcoded credentials.
- **Observable** — test results, coverage, and build logs are accessible.

## Docker image strategy

- Base image: `mcr.microsoft.com/dotnet/aspnet:10.0` for runtime.
- Build image: `mcr.microsoft.com/dotnet/sdk:10.0` for compilation.
- Registry: GitHub Container Registry (`ghcr.io`) by default.
- Tags: `latest` (main branch), commit SHA, semantic version.
