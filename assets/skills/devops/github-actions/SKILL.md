---
name: github-actions
description: >
  GitHub Actions CI/CD standards for .NET 10 projects. Covers build, test,
  and publish workflows, Docker image publishing, and artifact management.
  Use when creating or modifying any GitHub Actions workflow file.
---

# GitHub Actions — TemperAI Standards

## CI workflow — build, test, publish

```yaml
name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

env:
  DOTNET_VERSION: "10.0"

jobs:
  build:
    name: Build and Test
    runs-on: ubuntu-latest

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Run tests
        run: dotnet test --no-build --configuration Release --verbosity normal --logger trx --results-directory TestResults

      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: TestResults/

  publish:
    name: Publish Docker Image
    needs: build
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main' && github.event_name == 'push'

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Log in to GitHub Container Registry
        uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Build and push Docker image
        uses: docker/build-push-action@v5
        with:
          context: .
          push: true
          tags: |
            ghcr.io/${{ github.repository }}:latest
            ghcr.io/${{ github.repository }}:${{ github.sha }}
          cache-from: type=gha
          cache-to: type=gha,mode=max
```

## Workflow rules

- Always separate build/test and publish into different jobs.
- Always run tests before publishing — the publish job must depend on the build job.
- Always use GitHub Container Registry (`ghcr.io`) by default.
- Always tag with both `latest` and the commit SHA.
- Always upload test results as artifacts for debugging.
- Always use `--no-restore` after a restore step and `--no-build` after a build step.
- Always use `ubuntu-latest` as the runner unless the project specifies otherwise.
- Always pin action versions to a major version (e.g., `@v4`, `@v3`).

## Branch protection

- `main` branch — production-ready code, protected, requires PR and passing CI.
- `develop` branch — integration branch, triggers build and test but not publish.
- Feature branches — named `feature/description`, branch off `develop`.

## Secrets management

- Never hardcode secrets in workflow files.
- Always use `${{ secrets.SECRET_NAME }}` for sensitive values.
- Required secrets: `GITHUB_TOKEN` (automatic), registry credentials, connection strings if needed.
