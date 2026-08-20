// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Immutability;

// Happy path for Metalama.Patterns.Immutability. [Immutable] is a compile-time marker
// consumed by other aspects rather than a transformation, so the meaningful assertions
// are that it compiles and that the marker really lands on the emitted type.

[Immutable]
internal class Point( int x, int y )
{
    public int X { get; } = x;

    public int Y { get; } = y;
}

internal static class Program
{
    public static int Main()
    {
        var point = new Point( 3, 4 );

        PackageTestCheck.That( point is { X: 3, Y: 4 }, "the [Immutable] type behaves normally" );

        PackageTestCheck.That(
            typeof(Point).GetCustomAttributes( typeof(ImmutableAttribute), false ).Length == 1,
            "[Immutable] is present on the emitted type" );

        return PackageTestCheck.Report( "Metalama.Patterns.Immutability" );
    }
}
