# TemperAI Community Release Model

This document explains how public/community TemperAI releases are delivered and updated.

For the full maintainer policy, see [distribution-operations.md](distribution-operations.md).

## What community users install

Community users install a **global self-contained `temper-ai` CLI**.

- No local source checkout is required.
- No .NET SDK is required for normal installation.
- The default community setup uses **remote assets**.

## Remote assets by default

TemperAI ships its executable and its runtime assets separately.

For public installs:

- the CLI is installed globally
- agents, skills, templates, and related runtime assets are resolved from the published remote release assets
- the release manifest defines which CLI and asset package belong together

## Local source mode

TemperAI also supports **local source mode** for contributors and development workflows.

Use local source mode when you are:

- developing TemperAI itself
- validating unreleased asset changes
- testing against a local repository checkout

Local source mode is not the default public/community experience.

## How installation works

The supported public install path is the **one-line PowerShell bootstrap command** published with the release.

That bootstrap flow:

1. resolves the current `stable` release manifest
2. installs the self-contained CLI globally for the current user
3. configures TemperAI to use remote assets by default

## How updates work

Community users update TemperAI with one command:

```powershell
temper-ai update
```

This is the single user-facing update action. It updates both:

- the installed CLI
- the release assets for that installation

Users should not need separate CLI and asset update commands.

## Release manifest requirement

Every public release must include a **manifest**.

The manifest is the source of truth for:

- release version
- release channel
- CLI artifact
- assets artifact
- compatibility between the published CLI and assets

If a public release does not have a valid manifest, it is not a complete supported release.

## Release channel

The initial public release channel is:

- `stable`

## Primary user experience

Running TemperAI without arguments keeps the interactive menu as the primary experience:

```powershell
temper-ai
```

This remains true for community installs even though assets are resolved remotely by default.
