// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using BuildMetalamaTestsDotNetSdk.Helpers;
using JetBrains.Annotations;
using PostSharp.Engineering.BuildTools.Utilities;
using Spectre.Console.Cli;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BuildMetalamaTestsDotNetSdk.Commands;

// https://github.com/dotnet/sdk/issues/42638
[UsedImplicitly]
internal class SetAssemblyLocatorRefVersionCommand : AsyncCommand
{
    public override async Task<int> ExecuteAsync( CommandContext context )
    {
        var cts = new CancellationTokenSource();
        
        Console.CancelKeyPress += ( s, e ) =>
        {
            Console.WriteLine( "Canceling..." );
            cts.Cancel();
            e.Cancel = true;
        };
        
        var console = new ConsoleHelper();

        var versions = await NuGetHelper.GetVersionsAsync( console, "Microsoft.NETCore.App.Ref", false, cts.Token );

        var version = versions.Where( v => v is { Major: 6, Minor: 0 } ).MaxBy( v => v )?.ToString();
        
        if ( version == null )
        {
            console.WriteError( "Failed to get the version of 'Microsoft.NETCore.App.Ref' package." );
            return 1;
        }

        var directory = Path.Combine( Path.GetTempPath(), "Metalama", "AssemblyLocator" );

        Directory.CreateDirectory( directory );

        var file = Path.Combine( directory, "Directory.Build.props" );
        
        console.WriteMessage( $"Setting the version of 'Microsoft.NETCore.App.Ref' package to '{version}' in '{file}'." );

        await File.WriteAllTextAsync( file, @$"<Project>
  <PropertyGroup>
    <RuntimeFrameworkVersion Condition=""'$(TargetFramework)' == 'net6.0'"">{version}</RuntimeFrameworkVersion>
  </PropertyGroup>

  <ItemGroup>
    <KnownFrameworkReference  Update=""@(KnownFrameworkReference)"">
      <TargetingPackVersion Condition=""'%(TargetFramework)' == 'net6.0'"">{version}</TargetingPackVersion >
    </KnownFrameworkReference >
  </ItemGroup>
</Project>", cts.Token );
        
        return 0;
    }
}