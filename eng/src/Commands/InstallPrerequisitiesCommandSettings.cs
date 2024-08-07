// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using PostSharp.Engineering.BuildTools;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace BuildMetalamaTestsDotNetSdk.Commands;

internal class InstallPrerequisitiesCommandSettings : CommonCommandSettings
{
    [Description( "The type of project, e.g. console, blazor, maui, web, etc." )]
    [CommandArgument( 0, "<project-type>" )]
    public string ProjectType { get; init; } = null!;
}