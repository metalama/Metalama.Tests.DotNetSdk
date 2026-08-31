// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Contracts;

// Happy path for Metalama.Patterns.Contracts: a [NotNull] parameter contract must be
// woven into the method so that passing null throws instead of reaching the body.

internal class Greeter
{
    public static string Greet( [NotNull] string name ) => $"Hello, {name}!";
}

internal static class Program
{
    public static int Main()
    {
        PackageTestCheck.That( Greeter.Greet( "world" ) == "Hello, world!", "a valid argument passes the contract" );

        PackageTestCheck.Throws<ArgumentNullException>(
            () => Greeter.Greet( null! ),
            "[NotNull] rejects a null argument" );

        return PackageTestCheck.Report( "Metalama.Patterns.Contracts" );
    }
}
