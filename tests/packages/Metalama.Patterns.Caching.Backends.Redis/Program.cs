// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Backends.Redis;

// Happy path for Metalama.Patterns.Caching.Backends.Redis, minus the server.
//
// A real feature assertion would need a live Redis instance, which no matrix cell has
// (and standing one up on six operating systems is well beyond what this repository is
// for). So this test goes as far as it can without a network: it configures the backend,
// which is the part that has to load the package's types and run its code.

internal static class Program
{
    public static int Main()
    {
        var configuration = new RedisCachingBackendConfiguration { KeyPrefix = "package-test" };

        PackageTestCheck.That( configuration.KeyPrefix == "package-test", "the Redis backend configuration is usable" );

        return PackageTestCheck.Report( "Metalama.Patterns.Caching.Backends.Redis" );
    }
}
