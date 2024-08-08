// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using BuildMetalamaTestsDotNetSdk.Helpers;
using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BuildMetalamaTestsDotNetSdk.Commands;

internal class SetDotNetSdkVersionCommand : BaseCommand<SetDotNetSdkVersionCommandSettings> 
{
    protected override bool ExecuteCore( BuildContext context, SetDotNetSdkVersionCommandSettings settings )
    {
        context.Console.WriteHeading( $"Setting the .NET SDK version to {settings.Version}." );
        
        var path = Path.Combine( context.RepoDirectory, "global.json" );
        var json = File.ReadAllText( path );
        var o = JsonNode.Parse( json );
        o!["sdk"]!["version"] = settings.Version;
        json = JsonSerializer.Serialize( o, new JsonSerializerOptions { WriteIndented = true } );
        File.WriteAllText( path, json );

        DotNetInvocationHelper.Run( context, "", "--info" );
        
        context.Console.WriteSuccess( $"The .NET SDK version set to {settings.Version}." );

        return true;
    }
}