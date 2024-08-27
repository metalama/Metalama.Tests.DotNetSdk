// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using BuildMetalamaTestsDotNetSdk.Helpers;
using JetBrains.Annotations;
using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build;
using System;

namespace BuildMetalamaTestsDotNetSdk.Commands;

[UsedImplicitly]
internal class InstallPrerequisitiesCommand : BaseCommand<InstallPrerequisitiesCommandSettings>
{
    protected override bool ExecuteCore( BuildContext context, InstallPrerequisitiesCommandSettings settings )
    {
        context.Console.WriteHeading( $"Installing prerequisites for '{settings.ProjectType}' project type." );

        switch ( settings.ProjectType )
        {
            case "maui":
            case "maui-blazor":
                if ( !InstallMaui( context ) )
                {
                    return false;
                }

                break;
            default:
                context.Console.WriteImportantMessage( $"'{settings.ProjectType}' project type requires no prerequisites." );
                break;
        }

        context.Console.WriteSuccess( $"Prerequisites for '{settings.ProjectType}' project type installed." );

        return true;
    }

    private static bool InstallMaui( BuildContext context )
    {
        if ( OperatingSystem.IsWindows() )
        {
            if ( !InstallWorkload( context, "maui" ) )
            {
                return false;
            }
        }
        else
        {
            if ( !InstallWorkload( context, "maui-android" ) )
            {
                return false;
            }
            
            if ( !InstallWorkload( context, "maui-tizen" ) )
            {
                return false;
            }
        }

        return true;
    }

    private static bool InstallWorkload( BuildContext context, string workload )
    {
        context.Console.WriteImportantMessage( $"Installing workload '{workload}'." );

        return DotNetInvocationHelper.Run( context, "workload", $"install {workload}" );
    }
}