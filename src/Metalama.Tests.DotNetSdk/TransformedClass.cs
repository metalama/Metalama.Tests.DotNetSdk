// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Tests.DotNetSdk;

internal class TransformedClass
{
    [AlterResult]
    public int TransformedMethod()
    {
        return 41;
    }
}