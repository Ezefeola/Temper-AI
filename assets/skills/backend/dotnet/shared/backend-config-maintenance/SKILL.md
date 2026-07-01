---
name: backend-config-maintenance
description: >
  How the backend agent keeps the Dependencies section of
  Docs/Application/Architecture/backend-config.md in sync with the real project.
  Load only when a task adds, removes, or upgrades a NuGet package. Reconciles the
  Dependencies list from the actual .csproj PackageReference entries — never touches
  the architect's decision fields.
requires: [backend-dotnet-csharp]
---

# Backend Config Maintenance — TemperAI

`Docs/Application/Architecture/backend-config.md` is read by every downstream agent to know
the stack and installed packages. Its **Dependencies** section drifts the moment a package is
installed and not recorded. This skill defines how the backend agent keeps that section true
to the real project, using the `.csproj` files as the single source of truth.

## 🚨 NON-NEGOTIABLE RULES — ZERO TOLERANCE

1. **ONLY edit the `Dependencies:` block** of `backend-config.md`. Every other field
   (`Framework`, `Language`, `ORM`, `Data Access`, `Architecture`, `Database`, `Auth`, `API Docs`,
   `Health Checks`, `Messaging`, `Caching`, `Logging`) is owned by the architect and is
   **read-only** for you.
2. **RECONCILE from the real `.csproj`, never from memory.** Derive the list from the actual
   `<PackageReference>` entries, not from "what I think I added". This self-heals prior drift.
3. **NEVER invent or guess versions.** Use the exact `Version` from the `.csproj`. If a version
   is centrally managed (`Directory.Packages.props`), read it from there.
4. **NEVER touch a decision field even if it looks wrong.** If reality contradicts a decision
   field (e.g. the real ORM package is not the one in `ORM:`), STOP and ask — do not "fix" it.
5. **ALWAYS preserve the exact template format** of the Dependencies block (package name +
   version, one per line). Do not add justification text — this list is purely technical.

## When this skill applies

Load and run this reconciliation **only** when the task added, removed, or upgraded a NuGet
package (i.e. the task changed a `.csproj`). If the task touched no packages, do nothing —
leave `backend-config.md` untouched.

## Which projects count

- Include packages referenced by the **application/runtime projects** (the backend solution's
  source projects — API, Application, Domain, Infrastructure, etc.).
- **Exclude test-only projects** (anything referenced only by `*.Tests`/`*.UnitTests` projects).
  Test frameworks belong to the test setup, not the runtime config.
- Exclude analyzers and build-only `PrivateAssets="all"` tooling references unless the task
  added one deliberately as a runtime concern.

## Reconciliation procedure

1. **Locate the runtime `.csproj` files** of the backend solution (skip test projects).
2. **Read every `<PackageReference Include="..." Version="..." />`** across them. If versions
   are centrally managed, resolve each from `Directory.Packages.props`.
3. **Build the deduplicated, sorted set** of `Package Name + version` (alphabetical by name).
4. **Open `Docs/Application/Architecture/backend-config.md`** and replace the contents of the
   `Dependencies:` block with that set, keeping the exact template shape:
   ```
   Dependencies:
     - MailKit 4.8.0
     - ClosedXML 0.104.1
   ```
   If the runtime projects reference no third-party packages beyond the base stack, write:
   ```
   Dependencies:
     - None beyond base stack
   ```
5. **Leave every other line of the file byte-for-byte unchanged.**
6. If a decision field clearly contradicts the real packages, do NOT edit it — emit the
   stop-and-ask report and let the orchestrator route it to the architect.

## Completion note

After reconciling, report it in the backend agent's completion summary, e.g.:

```
🗂️ backend-config.md synced — Dependencies reconciled from .csproj
   Added:   [pkg version, …]
   Removed: [pkg, … or "none"]
```
