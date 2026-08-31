// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.Multicast;
using Metalama.Framework.Aspects;

[assembly: CountCalls( AttributeTargetTypes = "Work*" )]

// Happy path for Metalama.Extensions.Multicast: one assembly-level attribute must fan the
// aspect out to every matching method, rather than being applied one method at a time.

internal static class Counter
{
    public static int Value;
}

internal class CountCallsAttribute : OverrideMethodMulticastAspect
{
    public override dynamic? OverrideMethod()
    {
        Counter.Value++;

        return meta.Proceed();
    }
}

internal class Worker
{
    public void First() { }

    public void Second() { }
}

internal static class Program
{
    public static int Main()
    {
        var worker = new Worker();
        worker.First();
        worker.Second();

        PackageTestCheck.That( Counter.Value == 2, "the multicast aspect was applied to both methods" );

        return PackageTestCheck.Report( "Metalama.Extensions.Multicast" );
    }
}
