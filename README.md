# Metalama.Tests.DotNetSdk

This repo contains compatibility tests of Metalama with various .NET SDKs.


## Status

[![Platform Integration Tests](https://github.com/metalama/Metalama.Tests.DotNetSdk/actions/workflows/test.yml/badge.svg)](https://github.com/metalama/Metalama.Tests.DotNetSdk/actions/workflows/test.yml)

📋 **[View Latest Test Summary Report](https://github.com/metalama/Metalama.Tests.DotNetSdk/issues/3)** - Detailed test results for develop/2025.1 branch

## Limitations

For the matrix of platforms covered by these tests and its exclusions, see the [Platform Integration Tests](.github/workflows/test.yml) workflow.

Current exclusions (as of Metalama 2025.1) are:

- Blazor on .NET 10 SDK because its source generator requires Roslyn 5. Blazor 10 will be supported in Metalama 2026.0.

