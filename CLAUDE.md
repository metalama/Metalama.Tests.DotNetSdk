# CLAUDE.md

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
