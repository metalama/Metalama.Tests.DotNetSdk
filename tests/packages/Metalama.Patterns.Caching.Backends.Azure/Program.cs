// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Backends.Azure;

// Happy path for Metalama.Patterns.Caching.Backends.Azure, minus the service.
//
// This package provides a cache SYNCHRONIZER over Azure Service Bus, so a real assertion
// would need a live namespace and credentials. As with the Redis backend, the test goes
// as far as it can offline: it constructs the synchronizer configuration, which loads the
// package's types and runs its code.

internal static class Program
{
    public static int Main()
    {
        const string connectionString = "Endpoint=sb://example.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v";

        var configuration = new AzureCacheSynchronizerConfiguration( connectionString, "package-test-topic" );

        PackageTestCheck.That( configuration != null, "the Azure cache synchronizer configuration is usable" );

        // The package's own validation runs, which proves its code executed rather than
        // merely resolving: a connection string without a topic must be rejected.
        PackageTestCheck.Throws<ArgumentNullException>(
            () => _ = new AzureCacheSynchronizerConfiguration( connectionString ),
            "a connection string without a topic name is rejected" );

        return PackageTestCheck.Report( "Metalama.Patterns.Caching.Backends.Azure" );
    }
}
