// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Tests.DotNetSdk;

public class MetalamaTests
{
    [Fact]
    public void TransformedCodeIsExecuted()
    {
        var result = new TransformedClass().TransformedMethod();
        Assert.Equal(42, result);
    }
}