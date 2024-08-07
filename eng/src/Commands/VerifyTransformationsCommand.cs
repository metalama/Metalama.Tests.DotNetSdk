// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using BuildMetalamaTestsDotNetSdk.Helpers;
using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace BuildMetalamaTestsDotNetSdk.Commands;

internal class VerifyTransformationsCommand : BaseCommand<VerifyTransformationsCommandSettings>
{
    protected override bool ExecuteCore( BuildContext context, VerifyTransformationsCommandSettings settings )
    {
        context.Console.WriteHeading( "Verifying transformations" );

        // https://stackoverflow.com/a/72811925

        var runtimeAssemblies = Directory.GetFiles( RuntimeEnvironment.GetRuntimeDirectory(), "*.dll" ).ToList();

        bool VerifyAssembly(string assemblyPath)
        {
            var assemblyDirectory = Path.GetDirectoryName( assemblyPath );
            var outputAssemblies = Directory.GetFiles( assemblyDirectory, "*.dll" );
            var allAssemblies = runtimeAssemblies.Concat( outputAssemblies ).ToList();

            var resolver = new PathAssemblyResolver( allAssemblies );
            using var context = new MetadataLoadContext( resolver );
            var assembly = context.LoadFromAssemblyPath( assemblyPath );

            return assembly.DefinedTypes.Any( type => type.Name == "MetalamaIntroducedClass" );
        }

        var assembliesToVerify = Directory.GetFiles(
            Path.Combine( context.RepoDirectory, ProjectInfo.ProjectDirectory, "bin" ),
            $"{ProjectInfo.ProjectName}.dll",
            SearchOption.AllDirectories );

        var success = true;

        foreach (var assembly in assembliesToVerify)
        {
            context.Console.WriteMessage( $"Verifying assembly '{assembly}'." );

            if ( VerifyAssembly( assembly ) )
            {
                context.Console.WriteImportantMessage( $"Assembly '{assembly}' was transformed by Metalama." );
            }
            else
            {
                context.Console.WriteError( $"Assembly '{assembly}' was not transformed by Metalama." );
                success = false;
            }
        }

        if ( success )
        {
            context.Console.WriteSuccess( "All assemblies were transformed by Metalama." );
        }

        return success;
    }
}
