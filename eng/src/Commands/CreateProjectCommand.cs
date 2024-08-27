// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using BuildMetalamaTestsDotNetSdk.Helpers;
using JetBrains.Annotations;
using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build;
using System;
using System.IO;
using System.Linq;

namespace BuildMetalamaTestsDotNetSdk.Commands;

[UsedImplicitly]
internal class CreateProjectCommand : BaseCommand<CreateProjectCommandSettings>
{
    protected override bool ExecuteCore( BuildContext context, CreateProjectCommandSettings settings )
    {
        context.Console.WriteHeading( $"Creating new '{settings.ProjectType}' project." );

        // We disable the implicit restore so we don't need to prepare the version file and download artifacts beforehand.
        var noRestoreFlag = settings.ProjectType switch
        {
            "maui" => "", // MAUI template doesn't support this flag, but it doesn't do implicit restore either.
            "maui-blazor" => "", // MAUI Blazor template doesn't support this flag, but it doesn't do implicit restore either.
            _ => " --no-restore"
        };

        if ( !DotNetInvocationHelper.Run( context, "new", $"{settings.ProjectType} -o {ProjectInfo.ProjectDirectory} -n {ProjectInfo.ProjectName}{noRestoreFlag}" ) )
        {
            return false;
        }

        if ( settings.ProjectType == "maui" || settings.ProjectType == "maui-blazor" )
        {
            var projectFilePath = Path.Combine( ProjectInfo.ProjectDirectory, $"{ProjectInfo.ProjectName}.csproj" );
            var project = File.ReadAllText( projectFilePath );

            var targetFramework = project.Split( '\n' ).First( l => l.Contains( "<TargetFrameworks>", StringComparison.Ordinal ) )
                .Replace( "<TargetFrameworks>", "", StringComparison.Ordinal )
                .Replace( "</TargetFrameworks>", "", StringComparison.Ordinal ).Trim().Split( ';' ).First().Split( '-' )
                .First();
            
            void ReplaceInProject( string original, string replacement )
            {
                context.Console.WriteMessage( $"Replacing '{original}' with '{replacement}' in the project." );
                var replacedProject = project.Replace( original, replacement, StringComparison.Ordinal );

                if ( replacedProject == project )
                {
                    context.Console.WriteError( $"'{original}' not found in the project." );
                }

                project = replacedProject;
            }
            
            // Building Mac apps is not supported on Linux.
            if ( OperatingSystem.IsLinux() )
            {
                ReplaceInProject(
                    $"<TargetFrameworks>{targetFramework}-android;{targetFramework}-ios;{targetFramework}-maccatalyst</TargetFrameworks>",
                    $"<TargetFrameworks>{targetFramework}-android</TargetFrameworks>" );
            }

            // Enable Tizen.
            ReplaceInProject(
                $"<!-- <TargetFrameworks>$(TargetFrameworks);{targetFramework}-tizen</TargetFrameworks> -->",
                $"<TargetFrameworks>$(TargetFrameworks);{targetFramework}-tizen</TargetFrameworks>" );
            
            File.WriteAllText( projectFilePath, project );
        }

        context.Console.WriteSuccess( $"New '{settings.ProjectType}' project created." );

        return true;
    }
}
