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

- `MACOS_MAUI_SDK_9_0`, `MACOS_MAUI_SDK_10_0` — the SDK version, which is also
  the workload-set version passed to `dotnet workload install --version`.
- `MACOS_MAUI_XCODE_8_0` / `_9_0` / `_10_0` — the Xcode the pinned workload
  requires (.NET 8 has no SDK pin; its iOS workload is frozen at Xcode 16.0,
  which is always on the image).

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
   update the matching `MACOS_MAUI_XCODE_*`.
4. When the runner image gains a newer Xcode, repeat — the pins can then move up
   to a newer macios release.

This is deliberate manual maintenance: there is no way to be on the latest .NET
SDK and a working iOS/MacCatalyst workload at the same time during the gap
between an Xcode release and the runner image picking it up.
