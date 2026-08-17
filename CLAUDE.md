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

- Ubuntu backports PPA uses `dotnetX` naming (e.g., `dotnet8`, `dotnet9`)
- apt installs to `/usr/lib/dotnet`, setup-dotnet installs to `/usr/share/dotnet`
- GitHub API returns max 100 jobs per page - use pagination for large matrices
- when testing a new workflow, comment out the old test matrix, and only select the failing configurations. After success, restore the full matrix

## .NET 10 is not tested on this branch

The matrix covers .NET 8 and 9 only. Metalama 2026.0 does not support .NET 10:
its Metalama.Compiler bundles Roslyn 5.6, while the .NET 10.0.4xx SDK ships a
Razor generator built against Roslyn 5.9. Metalama detects the mismatch and
substitutes its own Razor generator from SDK 10.0.302 (`warning LAMA0617`), but
that substitution does not bring `Microsoft.Extensions.ObjectPool` 10.0.0 with
it, so every Razor-hosting project type then dies with:

```
error CS8785: Generator 'RazorSourceGenerator' failed to generate source ...
  'Could not load file or assembly 'Microsoft.Extensions.ObjectPool, Version=10.0.0.0 ...'
```

That was 19 red cells in run 31949157887 before .NET 10 was dropped. .NET 10 is
covered on `develop/2026.1`, whose Metalama carries Roslyn 5.9. **Do not add
`10.0` back to the matrix here** unless Metalama.Compiler 2026.0 moves to
Roslyn 5.9.

`ubuntu-22.04` was removed along with it: that runner image was in the matrix
only to get the `dotnet10` apt package, which Canonical does not publish for
24.04.

## Dependency seeding (keeping TeamCity artifacts off the company uplink)

The Metalama artifacts come from a self-hosted TeamCity on a LAN with limited
upstream bandwidth. Without seeding, every matrix job downloads them — around a
hundred pulls of the same payload per full run. Worse, the two branches are
dispatched simultaneously by `start-tests.yml`, so an unseeded branch saturates
the link for the other one too: on 2026-08-16 the unseeded 2026.0 matrix starved
2026.1's single seeding job, and *both* runs failed.

The workflow has two layers:

1. **`resolve-dependencies`** — resolves "latest Metalama build on this branch"
   **once**, downloads it, saves it to the Actions cache, and publishes
   `build-number`, `build-type-id` and `deps-id` as job outputs.
2. **`build`** — restores from that cache instead of downloading.

**Requires PostSharp.Engineering >= 2023.2.400** (`eng/Versions.props`) for the
`--cached-only` flag.

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

**The `build` jobs pass `--cached-only`**, so a cache miss *fails the job* instead
of silently downloading from TeamCity. This is deliberate — the silent fallback
hid a real bug twice on 2026.1: the run went green while every Windows job pulled
over the uplink. Build numbers are still resolved against TeamCity; only the
artifact download is forbidden. `resolve-dependencies` must **never** get this
flag: it is the job that populates the cache.

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
  `USERPROFILE=$HOME` on non-Windows.
- **The build number is pinned, not re-resolved.** Jobs use
  `dependencies set BuildServer Metalama --buildNumber N --buildTypeId T`. Beyond
  matching the cache key, this fixes a real hazard: when each job resolved the
  branch independently, a Metalama build completing mid-run silently split the
  matrix across two different Metalama versions.

Cache lifetime is GitHub's own — entries unused for 7 days are evicted and the
repo-wide 10 GB budget is reclaimed LRU. **There is no per-entry TTL to set**;
one week is already the platform behaviour.

**A failed seed job takes the whole run with it.** `build` declares
`needs: resolve-dependencies`, so a seeding failure skips every matrix cell —
that is what run 31949158055 on 2026.1 did. This is the intended trade: one
loud failure beats a hundred jobs hammering the uplink.

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

- `MACOS_MAUI_SDK_9_0` — the SDK version, which is also the workload-set version
  passed to `dotnet workload install --version`.
- `MACOS_MAUI_XCODE_8_0` / `_9_0` — the Xcode the pinned workload requires
  (.NET 8 has no SDK pin; its iOS workload is frozen at Xcode 16.0, which is
  always on the image).

There is no `_10_0` pin on this branch: .NET 10 is not tested here — see
".NET 10 is not tested on this branch" above.

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
   - The release's workload-set version band (e.g. `9.0.3xx`) must be a band
     `dotnet` can install — it just needs an SDK of the same band, which the
     pin itself provides.
3. From that release body, take the **workload set version** (e.g. `9.0.313`)
   and set the matching `MACOS_MAUI_SDK_*`. If the required Xcode changed, also
   update the matching `MACOS_MAUI_XCODE_*`.
4. When the runner image gains a newer Xcode, repeat — the pins can then move up
   to a newer macios release.

This is deliberate manual maintenance: there is no way to be on the latest .NET
SDK and a working iOS/MacCatalyst workload at the same time during the gap
between an Xcode release and the runner image picking it up.
