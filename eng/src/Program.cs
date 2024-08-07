// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using BuildMetalamaTestsDotNetSdk.Commands;
using BuildMetalamaTestsDotNetSdk.Helpers;
using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.Build.Solutions;
using PostSharp.Engineering.BuildTools.Dependencies.Definitions;
using Spectre.Console.Cli;
using MetalamaDependencies = PostSharp.Engineering.BuildTools.Dependencies.Definitions.MetalamaDependencies.V2024_2;

var product = new Product( MetalamaDependencies.DotNetSdkTests )
{
    Solutions = [new DotNetSolution( ProjectInfo.ProjectPath ) { BuildMethod = BuildMethod.Build }],
    Dependencies = [DevelopmentDependencies.PostSharpEngineering, MetalamaDependencies.Metalama]
};

var commandApp = new CommandApp();

commandApp.AddProductCommands( product );

commandApp.Configure( c => c.AddCommand<SetDotNetSdkVersionCommand>( "set-sdk-version" ).WithData( product ) );
commandApp.Configure( c => c.AddCommand<InstallPrerequisitiesCommand>( "install-prerequisities" ).WithData( product ) );
commandApp.Configure( c => c.AddCommand<CreateProjectCommand>( "create-project" ).WithData( product ) );

return commandApp.Run( args );