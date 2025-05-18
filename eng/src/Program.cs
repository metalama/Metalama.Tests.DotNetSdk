// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using BuildMetalamaTestsDotNetSdk.Commands;
using BuildMetalamaTestsDotNetSdk.Helpers;
using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.Build.Solutions;
using PostSharp.Engineering.BuildTools.Dependencies.Definitions;
using MetalamaDependencies = PostSharp.Engineering.BuildTools.Dependencies.Definitions.MetalamaDependencies.V2025_0;

var product = new Product( MetalamaDependencies.DotNetSdkTests )
{
    Solutions = [new DotNetSolution( ProjectInfo.ProjectPath ) { BuildMethod = BuildMethod.Build }],
    Dependencies = [DevelopmentDependencies.PostSharpEngineering, MetalamaDependencies.Metalama]
};

var app = new EngineeringApp( product );

var data = new BaseCommandData( product );
app.Configure( c => c.AddCommand<SetDotNetSdkVersionCommand>( "set-sdk-version" ).WithData( data ) );
app.Configure( c => c.AddCommand<CreateProjectCommand>( "create-project" ).WithData( data ) );
app.Configure( c => c.AddCommand<VerifyTransformationsCommand>( "verify-transformations" ).WithData( data ) );
app.Configure( c => c.AddCommand<SetAssemblyLocatorRefVersionCommand>( "set-ref-version" ).WithData( data ) );

return app.Run( args );