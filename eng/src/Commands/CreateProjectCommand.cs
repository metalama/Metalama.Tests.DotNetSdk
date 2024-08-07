// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using BuildMetalamaTestsDotNetSdk.Helpers;
using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build;

namespace BuildMetalamaTestsDotNetSdk.Commands;

internal class CreateProjectCommand : BaseCommand<CreateProjectCommandSettings>
{
    protected override bool ExecuteCore( BuildContext context, CreateProjectCommandSettings settings )
    {
        context.Console.WriteHeading( $"Creating new '{settings.ProjectType}' project." );

        if ( !DotNetInvocationHelper.Run( context, "new", $"{settings.ProjectType} -o {ProjectInfo.ProjectDirectory} -n {ProjectInfo.ProjectName}" ) )
        {
            return false;
        }

        context.Console.WriteSuccess( $"New '{settings.ProjectType}' project created." );

        return true;
    }
}
