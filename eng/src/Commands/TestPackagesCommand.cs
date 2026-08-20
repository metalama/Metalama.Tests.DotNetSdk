// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using BuildMetalamaTestsDotNetSdk.Helpers;
using JetBrains.Annotations;
using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BuildMetalamaTestsDotNetSdk.Commands;

/// <summary>
/// Builds and runs one small program per tested Metalama package.
/// </summary>
/// <remarks>
/// Each program exercises one obvious feature of its package and returns a non-zero exit
/// code if it did not work, so no test framework is involved: the same executable behaves
/// identically under every SDK version, SDK source and build tool in the matrix.
///
/// Every package is attempted even after one fails, because knowing that (say) only the
/// WPF package broke on a given SDK is far more useful than stopping at the first error.
/// </remarks>
[UsedImplicitly]
internal class TestPackagesCommand : BaseCommand<TestPackagesCommandSettings>
{
    protected override bool ExecuteCore( BuildContext context, TestPackagesCommandSettings settings )
    {
        context.Console.WriteHeading( $"Testing packages in '{settings.Directory}'." );

        var directory = Path.Combine( context.RepoDirectory, settings.Directory );

        if ( !Directory.Exists( directory ) )
        {
            context.Console.WriteError( $"The directory '{directory}' does not exist." );

            return false;
        }

        // One project per immediate subdirectory. This is deliberately NOT recursive:
        // 'tests/packages/Windows' is a sibling set selected by passing it explicitly,
        // so that the cross-platform run does not pick up the Windows-only projects.
        var projects = Directory.EnumerateDirectories( directory )
            .SelectMany( d => Directory.EnumerateFiles( d, "*.csproj" ) )
            .OrderBy( p => p )
            .ToList();

        if ( projects.Count == 0 )
        {
            context.Console.WriteError( $"No project was found under '{directory}'." );

            return false;
        }

        var frameworkArgument = string.IsNullOrEmpty( settings.TargetFramework )
            ? ""
            : $" -p:PackageTestTargetFramework={settings.TargetFramework}";

        var failed = new List<string>();

        foreach ( var project in projects )
        {
            var name = Path.GetFileNameWithoutExtension( project ).Replace( ".PackageTest", "", System.StringComparison.Ordinal );

            context.Console.WriteMessage( $"--- {name} ---" );

            if ( !Build( context, settings, project, frameworkArgument ) )
            {
                context.Console.WriteError( $"{name}: BUILD FAILED." );
                failed.Add( $"{name} (build)" );

                continue;
            }

            // `dotnet run` rather than invoking the executable directly: it resolves the
            // output path for us, which varies with the target framework (the WPF project
            // appends '-windows') and with the build tool that produced it.
            if ( !DotNetInvocationHelper.Run( context, "run", $"--project \"{project}\" --no-build{frameworkArgument}" ) )
            {
                context.Console.WriteError( $"{name}: TEST FAILED." );
                failed.Add( name );

                continue;
            }

            context.Console.WriteMessage( $"{name}: passed." );
        }

        context.Console.WriteHeading( "Package test summary" );

        if ( failed.Count > 0 )
        {
            context.Console.WriteError( $"{failed.Count} of {projects.Count} package(s) failed:" );

            foreach ( var name in failed )
            {
                context.Console.WriteError( $"  - {name}" );
            }

            return false;
        }

        context.Console.WriteSuccess( $"All {projects.Count} package(s) passed." );

        return true;
    }

    private static bool Build( BuildContext context, TestPackagesCommandSettings settings, string project, string frameworkArgument )
    {
        if ( settings.BuildTool == "dotnet" )
        {
            return DotNetInvocationHelper.Run( context, "build", $"\"{project}\"{frameworkArgument}" );
        }

        // MSBuild.exe does not restore implicitly, unlike `dotnet build`.
        return ToolInvocationHelper.InvokeTool(
            context.Console,
            "msbuild",
            $"\"{project}\" -restore{frameworkArgument}",
            context.RepoDirectory );
    }
}
