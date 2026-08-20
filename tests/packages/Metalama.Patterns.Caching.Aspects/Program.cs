// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching;
using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Caching.Building;

// Happy path for Metalama.Patterns.Caching.Aspects: [Cache] must intercept the method so
// that a second call with the same argument is served from the backend and the body runs
// only once. This exercises Metalama.Patterns.Caching and .Caching.Backend too, since the
// aspect is nothing without the service and the in-memory backend underneath it.

internal class PriceService
{
    public int CallCount;

    [Cache]
    public int GetPrice( string sku )
    {
        this.CallCount++;

        return sku.Length * 10;
    }
}

internal static class Program
{
    public static int Main()
    {
        using var cachingService = CachingService.Create( builder => builder.WithBackend( backend => backend.Memory() ) );

        // [Cache] introduces an ICachingService constructor parameter into the target type
        // (via Metalama.Extensions.DependencyInjection), so the service is passed in here.
        var service = new PriceService( cachingService );

        var first = service.GetPrice( "ABCD" );
        var second = service.GetPrice( "ABCD" );

        PackageTestCheck.That( first == 40, "the cached method returns the expected value" );
        PackageTestCheck.That( second == first, "the second call returns the same value" );
        PackageTestCheck.That( service.CallCount == 1, "[Cache] runs the method body only once" );

        return PackageTestCheck.Report( "Metalama.Patterns.Caching.Aspects" );
    }
}
