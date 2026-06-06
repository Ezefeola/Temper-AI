# TemperAI Distribution Operations

## Purpose

This document defines the approved public distribution and maintenance model for TemperAI.

It is the operational source of truth for maintainers and future implementation agents working on:

- packaging
- installation
- update flows
- release automation
- versioning
- manifest generation and consumption
- local development behavior

This document formalizes the approved strategy. It does not redesign that strategy.

## Scope

This policy applies to every public TemperAI release distributed through OpenCode-facing channels.

It covers:

- the CLI executable
- the remotely hosted TemperAI assets package
- the release manifest
- install and update behavior
- stable-channel publication
- local source mode for development

It does not define internal source layout beyond what is necessary to preserve the distribution contract.

## Approved Distribution Model

TemperAI is distributed as two separate deliverables:

1. A self-contained CLI executable.
2. A separate remote assets package.

Definitions:

- `CLI`: the `temper-ai` executable installed on the user's machine.
- `assets package`: the versioned bundle containing agents, skills, command assets, templates, and other non-executable runtime content required by TemperAI.
- `manifest`: the required metadata document that tells install and update operations which exact CLI and assets artifacts belong to a public release.

## Core Product Rules

The following rules are mandatory and must be preserved by all future implementations.

1. Public users install a global `temper-ai` CLI executable.
2. Public users use remote assets by default.
3. Local source mode remains available for development and contributor workflows.
4. A single logical update action updates both the CLI executable and the assets package.
5. Every public release includes a mandatory manifest.
6. The manifest is the source of truth for install and update.
7. Stable is the only initial public release channel.
8. Running `temper-ai` without arguments must continue to prioritize the interactive menu UX.

## User Experience Contract

### Install

The public installation path is a one-line PowerShell bootstrap command.

The bootstrap installer must:

- download or resolve the current stable manifest
- install the CLI globally for the current user
- configure the installed CLI so that community users default to remote assets
- complete without requiring the user to clone the repository

The installer is the supported public entry point for first-time installation.

### Runtime

When a user runs `temper-ai` with no arguments, the primary experience remains the interactive menu.

This rule is independent of whether assets were sourced remotely or from local source mode.

### Install Source Selection

Default behavior for community users:

- source mode = remote

Retained behavior for development:

- source mode = local
- local mode is intended for maintainers, contributors, and implementation work against the checked-out repository

Public installation must not default to local assets.

### Update

The user-facing update model is one action.

From the user's perspective, `temper-ai update` is one logical operation that brings the installation to the target public release state. That single action must update:

- the installed CLI executable
- the installed or configured assets version

The implementation may use multiple internal steps, but those steps must remain hidden behind one user action and one release manifest.

## Source Modes

TemperAI supports exactly two asset source modes.

### Remote Mode

Remote mode is the default for public/community installations.

In remote mode:

- the installed CLI resolves assets using release metadata from the public manifest
- the assets consumed by the user come from the published remote assets package, not from a local repository checkout
- install and update decisions are driven by the manifest for the selected channel and version

### Local Source Mode

Local source mode exists for development only.

In local source mode:

- the CLI is allowed to use assets from a local repository or working copy
- maintainers can test agent and skill changes before public publication
- local changes are not treated as a public release
- local mode must not weaken or bypass the manifest requirement for public releases

Local source mode is a development exception, not a second public distribution model.

## Manifest Policy

Every public TemperAI release must publish exactly one manifest for that released version.

The manifest is mandatory.

Without a valid manifest:

- public install is invalid
- public update is invalid
- the release is incomplete

### Manifest Responsibilities

The manifest must provide the canonical mapping between:

- product version
- channel
- CLI artifact identity
- assets artifact identity
- download locations
- integrity metadata required by the implementation

Installers and update flows must treat the manifest as authoritative. They must not hardcode release artifact pairs outside of manifest-driven logic.

### Manifest Minimum Required Fields

Every public manifest must define, at minimum:

- product name
- release version
- channel
- publication timestamp
- CLI artifact version
- CLI artifact download location
- assets package version
- assets package download location
- integrity data for each published artifact
- compatibility statement linking the CLI and assets package for that release

Optional fields may be added later, but required fields may not be removed from the public contract without replacing this policy.

### Manifest Example

The exact schema may evolve, but every public manifest must represent the same operational facts as the example below.

```json
{
  "product": "temper-ai",
  "version": "1.2.0",
  "channel": "stable",
  "publishedAt": "2026-06-06T12:00:00Z",
  "cli": {
    "version": "1.2.0",
    "platforms": [
      {
        "rid": "win-x64",
        "url": "https://github.com/Ezefeola/temper-ai/releases/download/v1.2.0/temper-ai-win-x64.zip",
        "sha256": "<cli-sha256>"
      }
    ]
  },
  "assets": {
    "version": "1.2.0",
    "url": "https://github.com/Ezefeola/temper-ai/releases/download/v1.2.0/temper-ai-assets-1.2.0.zip",
    "sha256": "<assets-sha256>"
  },
  "compatibility": {
    "cliVersion": "1.2.0",
    "assetsVersion": "1.2.0",
    "updateMode": "single-action"
  }
}
```

This example is normative in meaning, not in exact property names.

## Versioning Policy

Every public release must have one explicit product version.

That public product version governs:

- the CLI artifact version
- the assets package version
- the manifest version entry
- the release tag and release notes

### Version Alignment Rules

1. A public release version identifies one coherent TemperAI release state.
2. The manifest for that release must point to the exact CLI and assets artifacts that define that release state.
3. Public install and update must resolve to a matched CLI/assets pair declared by the manifest.
4. A public release may not publish an updated CLI without also publishing the release manifest.
5. A public release may not publish updated public assets without also publishing the release manifest.

### Stable Channel Rules

Initial public distribution supports only:

- `stable`

No other public channels are part of the approved initial strategy.

Future channels must not be introduced implicitly. They require an explicit product decision and a corresponding update to this document.

## Release Artifact Policy

Every public version must publish exactly the required artifacts for that version.

Required public release artifacts:

1. The self-contained CLI executable package for each supported public platform.
2. The remote assets package.
3. The release manifest.
4. The one-line PowerShell bootstrap installer entry point, or a bootstrap target that resolves the stable manifest and installs the CLI globally.

No public release is complete unless all required artifacts are published and mutually consistent.

### Artifact Relationship Rules

- The manifest binds the CLI artifact and the assets artifact into one release definition.
- Install resolves from the manifest to the matching artifacts.
- Update resolves from the manifest to the matching artifacts.
- The release process must not allow a public artifact to drift from its manifest.

## Installation Policy

### Public Install Flow

The supported public install flow is:

1. User runs one PowerShell bootstrap command.
2. Bootstrap resolves the current stable manifest.
3. Bootstrap installs the global CLI executable for the current user.
4. Installed CLI is configured for remote asset mode by default.
5. First run without arguments continues to present the interactive menu.

### Install Expectations

Public install must produce a usable TemperAI environment without requiring:

- repository clone
- local asset copying from source checkout
- manual artifact pairing by the user

The install flow must derive the release state from the manifest rather than from hardcoded artifact assumptions.

## Update Policy

### Single-Action Update Contract

The user-visible update command is one action that updates both distributable parts:

- CLI
- assets

This is a product invariant.

The user must not be required to:

- run one command for CLI updates and a second command for assets updates
- manually download a second package after updating the executable
- reason about CLI/assets pairing

### Update Resolution Rules

Update logic must:

1. resolve the target stable manifest
2. determine whether the installed release differs from the target release
3. fetch and apply the matching CLI and assets artifacts declared by that manifest
4. leave the installation in a coherent release state recorded against that manifest version

If any required artifact for the target release is unavailable or invalid, the update must be treated as failed or incomplete rather than partially successful.

### Partial Update Prohibition

For public stable releases, the system must not intentionally leave users in a state where:

- the CLI is updated to release `X`
- the assets remain on public release `Y`
- and the manifest does not explicitly define that combination as valid

The approved strategy is release-pair integrity, not best-effort independent public upgrades.

## Maintenance Policy

This section explains how changes propagate to user machines.

### Agent and Skill Changes

Changes to agents, skills, templates, commands, and other non-executable assets propagate to users through the published remote assets package of a public release.

Propagation path:

1. changes are merged in source
2. a new public version is prepared
3. a new assets package is published
4. a new manifest is published for that version
5. user machines receive the new assets through the single update operation

Agent or skill changes intended for public users must not rely on users pulling source code from the repository.

### CLI Changes

CLI behavior changes propagate to users through the published self-contained executable package referenced by the release manifest.

Propagation path:

1. CLI changes are merged in source
2. a new CLI artifact is published
3. the matching manifest is published
4. user machines receive the updated CLI through the single update operation

### Combined Release Changes

If a change requires both CLI behavior and assets changes, both must ship in the same public release definition and be linked by the same manifest.

This is the default expectation for any change that affects:

- asset loading rules
- agent loading behavior
- command behavior tied to asset structure
- compatibility-sensitive runtime behavior

## Local Development Policy

Local source mode must remain available.

Approved local development scenarios include:

- testing unreleased agent changes
- testing unreleased skill changes
- validating CLI behavior against working-copy assets
- contributor workflows before packaging a public release

Local development rules:

1. Local source mode is for development and validation, not for the default public install path.
2. Local source mode does not replace the requirement to publish a manifest for every public release.
3. Public support expectations are defined by stable manifest-backed releases, not by arbitrary local checkouts.
4. Development conveniences must not erode the public remote-first behavior.

## Constraints And Invariants

Future implementation agents must preserve all of the following.

### Distribution Invariants

1. TemperAI public distribution consists of a self-contained CLI plus a separate remote assets package.
2. Community installs default to remote assets.
3. Local source mode remains available for development.
4. Stable is the only initial public channel.

### UX Invariants

1. `temper-ai` with no arguments continues to prioritize the interactive menu.
2. Public installation remains a one-line PowerShell bootstrap flow.
3. Public update remains one logical user action.

### Release Invariants

1. Every public release must include a manifest.
2. The manifest is the source of truth for install and update.
3. Every public release must publish the complete required artifact set.
4. CLI and assets must be published as a coherent release pair referenced by the manifest.

### Safety Invariants

1. Public update must not knowingly strand users on an undefined CLI/assets combination.
2. Public install and update must fail clearly if the manifest or required artifacts are unavailable or invalid.
3. Future optimizations must not bypass manifest-driven resolution.

## Release Checklist

Use this checklist for every public stable release.

1. Confirm the release version to publish.
2. Confirm that the version represents one coherent TemperAI release state.
3. Build the self-contained CLI artifact for each supported public platform.
4. Build the remote assets package for that same release version.
5. Generate the release manifest that binds the CLI artifact and assets package together.
6. Verify manifest contents: version, channel, artifact URLs, and integrity values.
7. Verify the manifest points only to artifacts for the intended release version.
8. Publish the CLI artifact packages.
9. Publish the assets package.
10. Publish the manifest.
11. Verify the PowerShell bootstrap flow resolves the correct stable manifest.
12. Verify a fresh install uses remote mode by default.
13. Verify `temper-ai` with no arguments still opens the interactive menu.
14. Verify one update action updates both CLI and assets.
15. Verify the installed system reports a coherent post-update release state.
16. Publish release notes that reference the public version.
17. Do not mark the release complete unless all required artifacts are available and mutually consistent.

## Change Control

Any future change that would alter one of the following requires an explicit product decision and an update to this document before implementation is treated as complete:

- the two-part distribution model
- remote-by-default installation
- the single-action update contract
- the mandatory manifest policy
- the stable-only initial channel policy
- the interactive menu as the primary no-argument UX

Until such a decision is recorded, implementation work must conform to this document.
