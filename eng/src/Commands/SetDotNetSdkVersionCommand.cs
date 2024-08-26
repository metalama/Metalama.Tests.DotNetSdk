// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using BuildMetalamaTestsDotNetSdk.Helpers;
using JetBrains.Annotations;
using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BuildMetalamaTestsDotNetSdk.Commands;

[UsedImplicitly]
internal class SetDotNetSdkVersionCommand : BaseCommand<SetDotNetSdkVersionCommandSettings> 
{
    protected override bool ExecuteCore( BuildContext context, SetDotNetSdkVersionCommandSettings settings )
    {
        context.Console.WriteHeading( $"Setting the .NET SDK version to {settings.Version}." );
        
        var versionParts = settings.Version.Split( '-' );
        
        // On Linux, the dotnet-install.ps1 returns a version with "-servicing.<build>" suffix, but the SDK version is without it.
        var version = versionParts.Length > 1 && versionParts[1].StartsWith( "servicing", StringComparison.Ordinal )
            ? versionParts[0]
            : settings.Version;

        if ( version != settings.Version )
        {
            context.Console.WriteImportantMessage( $"Overriding version {settings.Version} to {version}" );
        }
        
        var path = Path.Combine( context.RepoDirectory, "global.json" );
        var json = File.ReadAllText( path );
        var o = JsonNode.Parse( json );
        o!["sdk"]!["version"] = version;
        json = JsonSerializer.Serialize( o, new JsonSerializerOptions { WriteIndented = true } );
        File.WriteAllText( path, json );
        
        context.Console.WriteSuccess( $"The .NET SDK version set to {version}." );

        DotNetInvocationHelper.Run( context, "", "--info" );

        return true;
    }
}