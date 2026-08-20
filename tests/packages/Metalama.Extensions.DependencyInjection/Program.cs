// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.DependencyInjection;
using Metalama.Framework.Aspects;

// Happy path for Metalama.Extensions.DependencyInjection: an aspect declaring a dependency
// must have it introduced into the target type and supplied through its constructor.

internal interface IGreetingService
{
    string Greet();
}

internal sealed class GreetingService : IGreetingService
{
    public string Greet() => "hello";
}

internal class GreetingAspectAttribute : TypeAspect
{
    [IntroduceDependency]
    private readonly IGreetingService _greetingService = null!;

    [Introduce]
    public string SayHello() => this._greetingService.Greet();
}

[GreetingAspect]
internal partial class Consumer;

internal static class Program
{
    public static int Main()
    {
        var consumer = new Consumer( new GreetingService() );

        PackageTestCheck.That( consumer.SayHello() == "hello", "the introduced dependency is injected and usable" );

        return PackageTestCheck.Report( "Metalama.Extensions.DependencyInjection" );
    }
}
