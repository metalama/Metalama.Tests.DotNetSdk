// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using BuildMetalamaTestsDotNetSdk.Helpers;
using JetBrains.Annotations;
using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build;

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

        context.Console.WriteSuccess( $"New '{settings.ProjectType}' project created." );

        return true;
    }
}
