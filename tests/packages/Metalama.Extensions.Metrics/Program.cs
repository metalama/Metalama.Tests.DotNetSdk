// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.Metrics;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Metrics;

// Happy path for Metalama.Extensions.Metrics: the aspect reads a compile-time metric of
// its target and bakes it into the generated code, so the value observed at run time
// proves the metric was actually computed during compilation.

internal class ReportStatementsAttribute : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        var statements = builder.Target.Metrics().Get<StatementsCount>().Value;

        builder.Advice.IntroduceMethod(
            builder.Target.DeclaringType,
            nameof(GetStatementCount),
            args: new { statements } );
    }

    [Template]
    public static int GetStatementCount( [CompileTime] int statements ) => statements;
}

internal partial class Analyzed
{
    [ReportStatements]
    public void ThreeStatements()
    {
        var a = 1;
        var b = 2;
        _ = a + b;
    }
}

internal static class Program
{
    public static int Main()
    {
        var actual = Analyzed.GetStatementCount();

        PackageTestCheck.That( actual == 3, $"the compile-time StatementsCount metric was 3 (was {actual})" );

        return PackageTestCheck.Report( "Metalama.Extensions.Metrics" );
    }
}
