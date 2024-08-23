// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using BuildMetalamaTestsDotNetSdk.Helpers;
using JetBrains.Annotations;
using NuGet.Versioning;
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
internal class SetAssemblyLocatorRefVersionCommand : AsyncCommand<SetAssemblyLocatorRefVersionCommandSettings>
{
    public override async Task<int> ExecuteAsync( CommandContext context,
        SetAssemblyLocatorRefVersionCommandSettings settings )
    {
        var console = new ConsoleHelper();
        
        // This version has to correspond to the version that AssemblyLocator uses.
        const int targetMajor = 6;
        const int targetMinor = 0;
        
        var sdkVersion = NuGetVersion.Parse( settings.SdkVersion );
        
        if (sdkVersion is { Major: targetMajor, Minor: targetMinor })
        {
            console.WriteMessage(
                $"No need to set the version of 'Microsoft.NETCore.App.Ref' package since we're using .NET {targetMajor}.{targetMinor} SDK." );

            return 0;
        }
        
        var cts = new CancellationTokenSource();

        Console.CancelKeyPress += ( _, e ) =>
        {
            Console.WriteLine( "Canceling..." );
            cts.Cancel();
            e.Cancel = true;
        };

        var versions = await NuGetHelper.GetVersionsAsync( console, "Microsoft.NETCore.App.Ref", false, cts.Token );

        var version = versions.Where( v => v is { Major: targetMajor, Minor: targetMinor } ).MaxBy( v => v )?.ToString();

        if ( version == null )
        {
            console.WriteError( "Failed to get the version of 'Microsoft.NETCore.App.Ref' package." );
            return 1;
        }

        var directory = Path.Combine( Path.GetTempPath(), "Metalama", "AssemblyLocator" );

        Directory.CreateDirectory( directory );

        var file = Path.Combine( directory, "Directory.Build.props" );

        console.WriteMessage(
            $"Setting the version of 'Microsoft.NETCore.App.Ref' package to '{version}' in '{file}'." );

        await File.WriteAllTextAsync( file, @$"<Project>
  <PropertyGroup>
    <RuntimeFrameworkVersion Condition=""'$(TargetFramework)' == 'net{targetMajor}.{targetMinor}'"">{version}</RuntimeFrameworkVersion>
  </PropertyGroup>

  <ItemGroup>
    <KnownFrameworkReference  Update=""@(KnownFrameworkReference)"">
      <TargetingPackVersion Condition=""'%(TargetFramework)' == 'net{targetMajor}.{targetMinor}'"">{version}</TargetingPackVersion >
    </KnownFrameworkReference >
  </ItemGroup>
</Project>", cts.Token );

        return 0;
    }
}