// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Memoization;

// Happy path for Metalama.Patterns.Memoization: [Memoize] must cache the result, so two
// reads of the property return the very same instance instead of two new ones.

internal class Registry
{
    public int EvaluationCount { get; private set; }

    [Memoize]
    public object Value
    {
        get
        {
            this.EvaluationCount++;

            return new object();
        }
    }
}

internal static class Program
{
    public static int Main()
    {
        var registry = new Registry();
        var first = registry.Value;
        var second = registry.Value;

        PackageTestCheck.That( ReferenceEquals( first, second ), "[Memoize] returns the same instance twice" );
        PackageTestCheck.That( registry.EvaluationCount == 1, "[Memoize] evaluates the getter only once" );

        return PackageTestCheck.Report( "Metalama.Patterns.Memoization" );
    }
}
