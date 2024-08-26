// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using PostSharp.Engineering.BuildTools;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace BuildMetalamaTestsDotNetSdk.Commands;

[UsedImplicitly]
internal class CreateProjectCommandSettings : CommonCommandSettings
{
    [Description( "The type of project, e.g. console, blazor, maui, web, etc." )]
    [CommandArgument( 0, "<project-type>" )]
    public string ProjectType { get; init; } = null!;
}
