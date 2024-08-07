// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.IO;

namespace BuildMetalamaTestsDotNetSdk.Helpers;

internal static class ProjectInfo
{
    public static string ProjectName { get; } = "Metalama.Tests.DotNetSdk";

    public static string ProjectDirectory { get; } = Path.Combine( "src", ProjectName );

    public static string ProjectPath { get; } = Path.Combine( ProjectDirectory, $"{ProjectName}.csproj" );
}
