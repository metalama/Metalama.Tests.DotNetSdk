// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using PostSharp.Engineering.BuildTools;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace BuildMetalamaTestsDotNetSdk.Commands;

[UsedImplicitly]
internal class SetDotNetSdkVersionCommandSettings : CommonCommandSettings
{
    [Description( "The version of the .NET SDK to be set." )]
    [CommandArgument( 0, "<version>" )]
    public string Version { get; init; } = null!;
}