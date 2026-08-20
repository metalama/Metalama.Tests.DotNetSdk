# Claude Code Session Learnings

## Repository Structure

- `.github/workflows/test.yml` - Main CI workflow for platform integration tests
- `Build.ps1` - PowerShell build script (generated from PostSharp.Engineering template)
- `eng/src/global.json` - Engineering SDK version configuration
- `eng/Versions.props` - Engineering package versions
- `GetLatestVersion.ps1` - Script to resolve exact .NET SDK versions

## Commit Guidelines

- Use short, descriptive commit messages
- Do NOT include Claude Code signature unless requested
- Example workflow:
```bash
git add <files>
git commit -m "Short descriptive message."
git push
```

## Continuous Status Monitoring

To check workflow status every 60 seconds until failure or completion:
```bash
# In a loop with sleep
sleep 60 && gh run view <run_id> --json status,conclusion,jobs --jq "{...}"
```

Or ask Claude to "check status every 60 seconds, stop on first failure, cancel build, analyze cause"

## Workflow Management

### Check workflow status
```bash
gh run view <run_id> --json status,conclusion,jobs --jq "{status: .status, conclusion: .conclusion, failed: [.jobs[] | select(.conclusion == \"failure\") | .name], completed: [.jobs[] | select(.status == \"completed\") | .name] | length, total: .jobs | length}"
```

### Trigger workflow
```bash
gh workflow run test.yml --ref develop/2026.0
```

### Cancel workflow
```bash
gh run cancel <run_id>
```

### Get job logs for analysis
```bash
# Get job ID
gh api repos/metalama/Metalama.Tests.DotNetSdk/actions/runs/<run_id>/jobs?per_page=100 --jq ".jobs[] | select(.name | contains(\"<job_name_fragment>\")) | .id"

# Get logs and search for errors
gh api repos/metalama/Metalama.Tests.DotNetSdk/actions/jobs/<job_id>/logs 2>&1 | grep -i "error\|fail"
```

## Cache Management

### List caches
```bash
gh cache list --limit 200 | grep ubuntu
```

### Delete specific cache by key
```bash
gh cache delete "build-100-ubuntu-24.04-apt-9.0.112-console"
```

## Common Issues and Solutions

### 1. Path separator issue on Linux
**Symptom**: Build.ps1 fails with path like `\eng\src\` on Linux
**Cause**: Windows-style backslashes in PowerShell script
**Fix**: Use `Join-Path` instead of string concatenation with backslashes
```powershell
# Bad
Set-Location $PSScriptRoot\$EngPath\src

# Good
Set-Location (Join-Path $PSScriptRoot $EngPath "src")
```

### 2. SDK version mismatch with apt
**Symptom**: Wrong SDK version used after apt installation
**Cause**: setup-dotnet overrides PATH priority
**Analysis**: Check which dotnet is being used and from where
**Fix**: Carefully manage PATH and DOTNET_ROOT environment variables

### 3. Engineering SDK requirement conflicts
**Symptom**: `NETSDK1045: The current .NET SDK does not support targeting .NET 9.0`
**Cause**: apt SDK (e.g., 8.0) taking precedence over engineering SDK (9.0)
**Fix**: Don't restore apt PATH priority; let engineering SDK handle Build.ps1, use global.json for test project

### 4. SDK resolution failure
**Symptom**: `sdk-not-found` error with exit code 155
**Cause**: `DOTNET_MULTILEVEL_LOOKUP=0` prevents finding SDKs from multiple locations
**Fix**: Remove `DOTNET_MULTILEVEL_LOOKUP=0` to allow multilevel SDK lookup

### 5. Cache causing stale behavior
**Symptom**: Code changes don't take effect
**Cause**: Jobs hitting old cache with previous (buggy) build artifacts
**Fix**: Delete relevant caches or bump CacheKeyPrefix

## Analyzing Workflow Failures

1. **Get the run status** to identify failed jobs
2. **Get the job ID** for the failed job
3. **Fetch logs** and search for error messages
4. **Look for context** around the error (use `-B5` with grep)
5. **Check environment**: SDK versions, PATH, environment variables
6. **Compare with working runs** to identify what changed

## apt SDK Testing Notes

- Ubuntu backports PPA uses `dotnetX` naming (e.g., `dotnet8`, `dotnet9`, `dotnet10`)
- .NET 10.0 apt package only available for Ubuntu 22.04 (Jammy), not 24.04 (Noble)
- apt installs to `/usr/lib/dotnet`, setup-dotnet installs to `/usr/share/dotnet`
- GitHub API returns max 100 jobs per page - use pagination for large matrices
- when testing a new workflow, comment out the old test matrix, and only select the failing configurations. After success, restore the full matrix

## Dependency seeding (keeping TeamCity artifacts off the company uplink)

The Metalama artifacts come from a self-hosted TeamCity on a LAN with limited
upstream bandwidth. Without seeding, every matrix job downloads them — 230 pulls
of the same payload per full run.

The workflow has two layers:

1. **`resolve-dependencies`** — resolves "latest Metalama build on this branch"
   **once**, downloads it, saves it to the Actions cache, and publishes
   `build-number`, `build-type-id` and `deps-id` as job outputs.
2. **`build`** — restores from that cache instead of downloading.

**One seeding job covers every platform.** The artifacts are plain NuGet packages —
no symlinks, no executable bits, nothing platform-specific — so the cache key
carries no `runner.os`/`runner.arch`. Do not re-introduce a per-platform seeding
matrix: it downloads the identical payload N times for no benefit.

**Sharing one cache entry across OSes needs two things, and both fail silently.**
A job that gets this wrong still passes — it just downloads from TeamCity and emits
a `::warning::`, which is easy to miss:

1. **`enableCrossOsArchive: true`** on every restore/save. Windows rejects entries
   written on another platform without it (`actions/cache` defaults it to `false`).
2. **A workspace-relative cached path** (`.deps-cache`). `actions/cache` cannot
   translate an absolute path such as `~/.build-artifacts` across operating
   systems: with `enableCrossOsArchive` it re-roots the archive at
   `GITHUB_WORKSPACE`, so a Linux-written entry unpacked on Windows lands in
   `D:\a\<repo>` rather than `C:\Users\runneradmin`, and nothing reads it.

Hence the staging directory: jobs cache `.deps-cache` and copy to/from
`$USERPROFILE/.build-artifacts` around it.

**The `build` jobs pass `--cached-only`** (PostSharp.Engineering >= 2023.2.400), so
a cache miss *fails the job* instead of silently downloading from TeamCity. This is
deliberate — the silent fallback hid a real bug twice: the run went green while
every Windows job pulled over the uplink. Build numbers are still resolved against
TeamCity; only the artifact download is forbidden. `resolve-dependencies` must
**never** get this flag: it is the job that populates the cache.

The trade-off is that a cache miss is now fatal rather than slow. If GitHub evicts
the entry mid-run, matrix jobs fail. Remove the flag to get the old degrade-to-slow
behaviour back.

**Verify a change here by grepping a Windows job log** for `Downloading` (must be 0)
and `was already downloaded` (must be 2) — a green run proves nothing on its own.

Why this works with **no PostSharp.Engineering changes**:

- `DependenciesHelper.DownloadBuild` writes a `.completed` sentinel next to the
  artifacts and **skips the download entirely when it exists**, so a
  cache-restored tree is reused verbatim.
- The absolute paths baked into `nuget.config` and `eng/Versions.*.g.props` do
  not need to survive the cache — every job regenerates them locally in
  `Build.ps1 prepare`. Only the artifacts themselves must be restored.

Two things to know:

- **`USERPROFILE` must be pinned on Unix.** The cache location is
  `Environment.GetEnvironmentVariable("USERPROFILE") ?? Path.GetTempPath()`
  (`DependenciesHelper.cs:584`). `USERPROFILE` never exists on Linux/macOS, and
  on macOS the fallback is a volatile per-session `/var/folders/...` path that
  cannot be cached. Every job that touches `~/.build-artifacts` therefore sets
  `USERPROFILE=$HOME` on non-Windows. If this is ever fixed properly in
  PostSharp.Engineering, `DependenciesHelper.cs:584` is a single isolated choke
  point — no other code reads that path.
- **The build number is pinned, not re-resolved.** Jobs use
  `dependencies set BuildServer Metalama --buildNumber N --buildTypeId T`. Beyond
  matching the cache key, this fixes a real hazard: when each job resolved the
  branch independently, a Metalama build completing mid-run silently split the
  matrix across two different Metalama versions.

Cache lifetime is GitHub's own — entries unused for 7 days are evicted and the
repo-wide 10 GB budget is reclaimed LRU. **There is no per-entry TTL to set**;
one week is already the platform behaviour. If the artifacts turn out to exceed
the 10 GB budget, move to Release assets or external TeamCity artifact storage
(S3/Azure Blob/R2) instead.

**A failed seed job takes the whole run with it.** `build` declares
`needs: resolve-dependencies`, so a seeding failure skips every matrix cell —
run 31949158055 lost all 232 that way. This is *not* best-effort, despite what
this file used to claim: that description belonged to an earlier design (commit
`34b0b66`) which had a separate best-effort `seed-dependencies` job alongside
the required resolver. `6e80c4b` collapsed the two into one, and `f3d9c26`
removed the cells' fallback as well. All-or-nothing is the coherent behaviour
now: under `--cached-only`, a failed seed means every cell would fail anyway,
and skipping 232 cells beats failing them.

Two consequences are handled explicitly:

- **The seeding download is retried** (3 attempts, 60 s apart). One dropped
  connection would otherwise cost the entire run, and the download is ~360 MB
  over a constrained uplink on an HttpClient with the default 100 s per-request
  timeout. Retrying is safe with no cleanup: `DownloadBuild` deletes the restore
  directory whenever `.completed` is absent, so a partial tree is discarded
  rather than reused.
- **An empty matrix is reported.** `generate-summary` has `if: always()`, so it
  also runs when `build` was skipped and there are no `build (...)` jobs to
  parse. It used to overwrite the summary issue with an empty report and open no
  failure issue — making a *total* failure the only kind that reported nothing.
  It now leaves the summary intact and opens a "the matrix never ran" issue.

The branches are also **staggered** in `start-tests.yml` rather than dispatched
together, so their seeding jobs never compete for the uplink.

## x86 SDK testing

The `setup-dotnet-x86` value of the `sdk-source` matrix axis tests the 32-bit
Windows .NET SDK. It is Windows-x64-host only (`windows-latest`): there is no
x86 SDK for `windows-11-arm`, and none at all for Linux/macOS.

It works by passing `architecture: x86` to `actions/setup-dotnet@v5`, which
installs into a dedicated `<PROGRAMFILES>\dotnet\x86` root and points
`DOTNET_ROOT`/`PATH` there. **Both** setup-dotnet steps (test SDK and
engineering SDK) must get the same `architecture` value — a dotnet host only
sees SDKs in its own root, so a mismatch makes one of the two invisible.

Every other step keys off `matrix.sdk-source != 'apt'`, so x86 jobs follow the
same path as the normal setup-dotnet jobs with no further special-casing.

To exercise only these jobs, dispatch the workflow with
`sdk-source: setup-dotnet-x86`.

**MAUI is excluded from x86**: `dotnet workload install maui` fails with
`Workload ID maui isn't supported on this platform` — Microsoft does not publish
MAUI workloads for win-x86. This is not fixable; the excludes are permanent.

## MSBuild.exe testing (the `build-tool` axis)

`build-tool` selects what drives the build: the `dotnet` CLI (baseline) or the
.NET Framework `MSBuild.exe` from Visual Studio. Both Windows runner images carry
Visual Studio, so `msbuild-x64`/`msbuild-x86` run on `windows-latest` and
`msbuild-arm64` on `windows-11-vs2026-arm`.

**Windows ARM uses the `windows-11-vs2026-arm` image, not `windows-11-arm`.** The
plain ARM image ships VS 2022 (MSBuild 17.14), and the .NET 10 SDK requires MSBuild
>= 18.0 — so `MSBuild.exe` there fails with `Could not resolve SDK
"Microsoft.NET.Sdk"`. `windows-latest` already carries MSBuild 18.7 despite what the
runner-image readme says; trust the job log over the readme. There is no
`MSBuild.exe` on Linux or macOS, so the axis is excluded there entirely.

MSBuild is located with `microsoft/setup-msbuild@v2`, which uses `vswhere`.
**Never hardcode the Visual Studio path** — the edition and version on the runner
image move.

Two things that are easy to get wrong:

1. **`msbuild-architecture` defaults to `x86`.** It maps to the host binary:
   `x86` → `MSBuild\Current\Bin\MSBuild.exe` (32-bit), `x64` →
   `Bin\amd64\MSBuild.exe`, `arm64` → `Bin\arm64\MSBuild.exe`. The VS docs saying
   "VS 2022 uses the 64-bit MSBuild" describe what the IDE launches, *not* what
   sits at `Bin\MSBuild.exe`. So `x64` must be requested explicitly, or you
   silently test a 32-bit host.
2. **`MSBuild.exe` does not restore implicitly**, unlike `dotnet build`. The
   build step passes `-restore`; without it the build fails on a missing
   `project.assets.json`.

`MSBuild.exe` still needs a .NET SDK on PATH to build SDK-style projects (it
honours `global.json`), so the `setup-dotnet` steps apply to these jobs too. This
axis varies the *build host*, not the SDK.

The axis is deliberately **not** fully crossed with `sdk-source`: MSBuild runs
against the normal x64 SDK, and the x86 SDK runs under `dotnet build` only.
Dropping the two `sdk-source: setup-dotnet-x86` / `build-tool: msbuild-*`
excludes would add the missing cells, at the cost of a much larger matrix.

To exercise only these jobs, dispatch with e.g. `build-tool: msbuild-x64`.

## Updating the macOS MAUI version pins

`.github/workflows/test.yml` builds MAUI projects on macOS. The iOS and Mac
Catalyst target frameworks require the active Xcode to match the Xcode that the
installed .NET iOS/MacCatalyst workload was built for, otherwise the build fails
with `error : This version of .NET for iOS (NN.N.xxxxx) requires Xcode NN.N`.

These macOS jobs cannot use the latest .NET SDK, because:

- The GitHub `macos-15` runner image lags Apple's Xcode releases by weeks, and
  `setup-xcode` can only *select* an Xcode already on the image — it cannot
  download one.
- Microsoft's iOS/MacCatalyst workloads lag the .NET SDK by a feature band, so
  the latest SDK has no matching iOS workload yet.

So the macOS MAUI jobs are **pinned** in the `env:` block at the top of
`test.yml`:

- `MACOS_MAUI_SDK_9_0`, `MACOS_MAUI_SDK_10_0`, `MACOS_MAUI_SDK_11_0` — the SDK
  version, which for the released bands is also the workload-set version passed
  to `dotnet workload install --version`.
- `MACOS_MAUI_WORKLOADSET_11_0` — the workload-set version, needed only where it
  differs from the SDK version. It does for the .NET 11 previews (SDK
  `11.0.100-preview.3.26207.106` ships with set `11.0.100-preview.3.26214.1`);
  for .NET 9/10 the two are identical, so those bands have no such variable and
  the install step reuses the SDK pin.
- `MACOS_MAUI_XCODE_8_0` / `_9_0` / `_10_0` / `_11_0` — the Xcode the pinned
  workload requires (.NET 8 has no SDK pin; its iOS workload is frozen at Xcode
  16.0, which is always on the image).

The pins lag intentionally. **Review them periodically** (roughly monthly, or
whenever a macOS MAUI job fails with a `requires Xcode` error):

1. Find the newest Xcode on the runner image — check the macos-15 readme:
   <https://github.com/actions/runner-images/blob/main/images/macos/macos-15-Readme.md>
   and note the highest `Xcode` version listed.
2. For each pinned .NET version, browse <https://github.com/dotnet/macios/releases>
   and find the **newest** release for that .NET major whose body says
   *"Xcode X.Y is required"* with `X.Y` **less than or equal to** the runner's
   newest Xcode.
   - Read the required Xcode from the release **body**, never the tag name —
     the tag is stale (e.g. tag `...xcode26.2...` whose body requires Xcode 26.3).
   - The release's workload-set version band (e.g. `10.0.2xx`) must be a band
     `dotnet` can install — it just needs an SDK of the same band, which the
     pin itself provides.
3. From that release body, take the **workload set version** (e.g. `10.0.202`)
   and set the matching `MACOS_MAUI_SDK_*`. If the required Xcode changed, also
   update the matching `MACOS_MAUI_XCODE_*`. If the workload-set version is not
   the SDK version — true for every preview band — set `MACOS_MAUI_SDK_*` from
   the SDK release and `MACOS_MAUI_WORKLOADSET_*` from the macios release body.
4. When the runner image gains a newer Xcode, repeat — the pins can then move up
   to a newer macios release.

This is deliberate manual maintenance: there is no way to be on the latest .NET
SDK and a working iOS/MacCatalyst workload at the same time during the gap
between an Xcode release and the runner image picking it up.


## .NET 11

.NET 11 is in **preview** (preview 7, `11.0.100-preview.7.26381.103`, as of
2026-08). Its axis value is `11.0`, and it behaves differently from the released
bands in four ways.

**Version resolution does not come from GitHub.** `GetLatestVersion.ps1` reads
the dotnet/sdk GitHub releases, but dotnet/sdk **stopped tagging .NET 11
previews after preview 2** (2026-03) while the product kept shipping one a
month — so `11.0` resolved to a five-month-old SDK. The script now falls back to
the official release metadata
(`https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json`,
the feed `dotnet-install` itself reads) whenever a channel's GitHub releases are
**all prereleases**, or when GitHub knows the channel at all. Stable channels are
untouched: they still resolve from GitHub, which is the only source here that
supports feature-band filtering (`9.0.3xx`) directly.

Do not "simplify" this by reading the metadata feed for every channel. The
GitHub path exists because `dotnet-install` mis-resolves suffixed versions, and
the metadata index has no notion of a feature band.

**There is no apt package.** The dotnet/backports PPA publishes `dotnet6`
through `dotnet10` only — Canonical does not package .NET previews. The whole
`sdk-source: apt` axis is therefore excluded for `11.0`, which in turn leaves
`ubuntu-22.04` (an apt-only runner here) with no `11.0` cells at all. Both
excludes come out when .NET 11 reaches GA and `dotnet11` appears in the PPA.

**macOS MAUI is pinned two previews further back than everything else.** The
.NET 11 macios releases move with Xcode fast: preview 4 and later require Xcode
26.4+, which requires macOS 26.2 — and the `macos-15` runner image tops out at
Xcode 26.3. `dotnet-11.0.1xx-preview3-11588` (Xcode 26.3) is the newest .NET 11
macios release this runner can host, so `MACOS_MAUI_SDK_11_0` sits at preview 3
while every other `11.0` cell tests preview 7. The summary table annotates the
deviating cells with their own SDK version, so this is visible rather than
silent.

This pin will **not** move forward when a newer preview ships — only when the
`os` axis gains a `macos-26` runner. That is the real fix and is deliberately
out of scope here: it changes every macOS cell, not just the .NET 11 ones.

**The matrix grows by ~29%.** 230 cells for 8/9/10, 297 with 11.0 added. That
only matters for the weekly scheduled runs on the develop branches; dispatching
with `dotnet-version: 11.0` selects the 67 `11.0` cells alone.

## The summary issue is only written by tracked branches

`generate-summary` writes to a single per-branch pseudo-issue
(`SUMMARY_ISSUE_NUMBER`, #7 on 2026.1) and opens a new issue for every failed
cell. Both are keyed to the branch's *constant*, not to the run, so a run on a
topic branch used to overwrite the develop branch's last good full-matrix report
with its own partial one — and file failure issues for a matrix nobody expected
to be green.

The job now writes to issues only when the branch starts with `develop/` or
`release/`. Every run, tracked or not, writes the same report to the **run
summary** (`core.summary`), so a topic-branch run is still fully readable from
the Actions page — it just leaves the issues alone.


## The Metalama dependency branch is not this repository's branch

`resolve-dependencies` asks TeamCity for "the latest Metalama build on branch X".
X used to be `github.ref_name` — this repository's own branch — which silently
assumes the branch exists in **both** repositories. That holds for the `develop/*`
and `release/*` lines and for nothing else.

Dispatching on a topic branch therefore failed outright:

```
Cannot get the last build for build type 'Metalama_Metalama20261_Metalama_DebugBuild',
branch 'topic/2026.1/net11': No build available.
```

Because `build` declares `needs: resolve-dependencies`, that skipped all 67
matrix cells (run 32359427586).

A topic branch now falls back to the develop branch its own name encodes —
`topic/2026.1/net11` → `develop/2026.1` — and the `metalama-branch` dispatch
input overrides that when a topic branch here *does* have a matching Metalama
branch to test against.

Worth knowing when a seeding failure costs you time: the download retry (3
attempts, 60 s apart) does not distinguish a dropped connection from a
deterministic failure, so an unresolvable branch burns two extra minutes before
reporting. The retry is load-bearing for the flaky-uplink case it was written
for, so it was left alone.


## Metalama.Patterns / Metalama.Extensions package tests

`tests/packages` holds one small console program per Metalama package: it exercises one
obvious feature and returns a non-zero exit code if it did not work. There is no test
framework on purpose — a plain executable behaves identically under every SDK version,
SDK source and build tool in the matrix. `./Build.ps1 test-packages` builds and runs them
all and prints a per-package summary.

**They add no matrix dimension.** The steps are gated on `matrix.project-type`, which is a
filter over existing cells:

- `console` cells run `tests/packages`. Those 41 cells already span **all 41** distinct
  `(os, dotnet-version, sdk-source, build-tool)` combinations in the 297-cell matrix, so
  this is full platform coverage — running them in every cell would repeat the same 41
  combinations nine times for nothing.
- `wpf` cells run `tests/packages/Windows`, which holds `Metalama.Patterns.Wpf` (it needs
  a `net*-windows` target framework and `UseWPF`). Those 24 cells cover every Windows
  combination.

If you add a package that only builds on Windows, put it under `tests/packages/Windows`.
Discovery is **not** recursive, so the cross-platform run never picks it up.

**Where the packages come from.** `Metalama.Premium` was always an explicit dependency of
this product; it simply had no source configured, which is what the long-standing
"A property named 'MetalamaPremiumVersion' must be defined" warning meant. Declaring that
property in `eng/AutoUpdatedVersions.props` lets it resolve. The split:

- The **`Metalama`** artifact — already downloaded by `resolve-dependencies` — carries
  Patterns.Contracts, .Memoization, .Observability, .Immutability, .Caching(+.Aspects,
  .Backend), .Wpf and Extensions.DependencyInjection(+.ServiceLocator), .Multicast,
  .Metrics, .DiffEngine, .HtmlWriter. `metalama/Metalama` is a monorepo, so these cost no
  extra download.
- **`Metalama.Premium`** carries Extensions.Architecture, .Validation, .CodeFixes and
  Patterns.Caching.Backends.Azure / .Redis. This is one extra seeding download.

**Four packages need a license key.** `VerifyMetalamaLicense` runs at build time for
Extensions.Validation, Extensions.CodeFixes, Caching.Backends.Azure and
Caching.Backends.Redis; the other thirteen have no license task. The key is the MSBuild
property `$(MetalamaLicense)`, supplied from the `METALAMA_LICENSE` secret. Without it
those four fail with `LAMA0806: The component '...' is not licensed`.

Note when testing this locally: a developer machine with a registered Metalama license
passes the check silently, so it does **not** reproduce CI. Add
`-p:MetalamaIgnoreUserLicenses=true` to get the CI behaviour.

**Not every package can be asserted at run time**, and the three exceptions are
deliberate:

- Extensions.Validation, .CodeFixes, .DiffEngine and .HtmlWriter ship **no `lib/` folder** —
  they are build-time extensions with no run-time API. Their test proves that referencing
  the package still produces a working Metalama build on that platform, which is exactly
  what this repository exists to check.
- Extensions.Architecture is a compile-time constraint; the test asserts that conforming
  code compiles and works. The violation case is not covered.
- Caching.Backends.Redis / .Azure would need live servers. Their tests go as far as they
  can offline, constructing the backend configuration.
