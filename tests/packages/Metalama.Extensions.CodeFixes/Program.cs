// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

// Metalama.Extensions.CodeFixes ships no lib/ folder: it is a build-time extension with no run-time API to call,
// so there is no feature to assert at run time. Its effect is offering code fixes in the IDE.
//
// What this project therefore proves is exactly what this repository exists to prove:
// that referencing the package on this OS, SDK version, SDK source and build tool still
// produces a working build. An aspect is applied so the compilation is a real Metalama
// compilation rather than a plain csc one.

internal class TraceAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Marker.Called = true;

        return meta.Proceed();
    }
}

internal static class Marker
{
    public static bool Called;
}

internal class Work
{
    [Trace]
    public int Compute() => 6 * 7;
}

internal static class Program
{
    public static int Main()
    {
        var result = new Work().Compute();

        PackageTestCheck.That( result == 42, "the project referencing Metalama.Extensions.CodeFixes builds and runs" );
        PackageTestCheck.That( Marker.Called, "Metalama actually wove the compilation" );

        return PackageTestCheck.Report( "Metalama.Extensions.CodeFixes" );
    }
}
