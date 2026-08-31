// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.Architecture.Aspects;

// Happy path for Metalama.Extensions.Architecture: an architecture rule is declared and
// conforming code compiles clean. The rule is a compile-time constraint, so "it works"
// means the constrained member is reachable from the permitted namespace and the build
// produced no diagnostic. (The violation case is not asserted here: that would need the
// build to expect a warning, which this harness deliberately keeps out of scope.)

namespace Allowed
{
    // The rule governs the INTERNAL members of a public type, so the type itself
    // must be public for the aspect to be applicable.
    [InternalsCanOnlyBeUsedFrom( Namespaces = ["Allowed"] )]
    public class Restricted
    {
        internal static int Value => 42;
    }

    internal static class Consumer
    {
        public static int Read() => Restricted.Value;
    }
}

internal static class Program
{
    public static int Main()
    {
        PackageTestCheck.That( Allowed.Consumer.Read() == 42, "the architecture-constrained member is usable from a permitted namespace" );

        return PackageTestCheck.Report( "Metalama.Extensions.Architecture" );
    }
}
