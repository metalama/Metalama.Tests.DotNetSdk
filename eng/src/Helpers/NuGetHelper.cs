// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using BuildMetalamaTestsDotNetSdk.Commands;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using PostSharp.Engineering.BuildTools.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BuildMetalamaTestsDotNetSdk.Helpers;

internal static class NuGetHelper
{
    public static async Task<IEnumerable<NuGetVersion>> GetVersionsAsync( ConsoleHelper console, string packageName,
        bool includePrerelease, CancellationToken cancellationToken )
    {
        var repository = Repository.Factory.GetCoreV3( "https://api.nuget.org/v3/index.json" );
        var resource = await repository.GetResourceAsync<PackageSearchResource>( cancellationToken );
        var searchFilter = new SearchFilter( includePrerelease: includePrerelease );

        // Making a search with taking one package.
        IEnumerable<IPackageSearchMetadata> searchResults = await resource.SearchAsync(
            packageName,
            searchFilter,
            skip: 0,
            take: 1,
            new NuGetCliLogger( console ),
            cancellationToken );

        var package = searchResults.SingleOrDefault();

        if ( package == null )
        {
            return [];
        }

        var versions = await package.GetVersionsAsync();

        return versions?.Select( v => v.Version ) ?? [];
    }
}