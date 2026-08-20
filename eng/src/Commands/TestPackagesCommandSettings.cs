// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using PostSharp.Engineering.BuildTools;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace BuildMetalamaTestsDotNetSdk.Commands;

[UsedImplicitly]
internal class TestPackagesCommandSettings : CommonCommandSettings
{
    [Description(
        "The directory holding one subdirectory per tested package. Defaults to 'tests/packages'. "
        + "Pass 'tests/packages/Windows' for the projects that only build on Windows." )]
    [CommandArgument( 0, "[directory]" )]
    public string Directory { get; init; } = "tests/packages";

    [Description( "The tool that drives the build: 'dotnet' (default) or 'msbuild'." )]
    [CommandOption( "--build-tool" )]
    public string BuildTool { get; init; } = "dotnet";

    [Description( "The target framework the test projects must build for, e.g. 'net10.0'." )]
    [CommandOption( "--target-framework" )]
    public string? TargetFramework { get; init; }
}
