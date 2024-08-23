// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using PostSharp.Engineering.BuildTools;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace BuildMetalamaTestsDotNetSdk.Commands;

public class SetAssemblyLocatorRefVersionCommandSettings : CommonCommandSettings
{
    [Description( "The version of .NET SDK. E.g. 6.0.302." )]
    [CommandArgument( 0, "<sdk-version>" )]
    public string SdkVersion { get; init; } = null!;
}