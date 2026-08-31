// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.DependencyInjection;
using Metalama.Extensions.DependencyInjection.ServiceLocator;
using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;

// Happy path for Metalama.Extensions.DependencyInjection.ServiceLocator: once the
// service-locator framework is registered, a dependency is resolved from the ambient
// IServiceProvider rather than pulled through the target's constructor.

internal interface IGreetingService
{
    string Greet();
}

internal sealed class GreetingService : IGreetingService
{
    public string Greet() => "hello";
}

internal sealed class SimpleServiceProvider : IServiceProvider
{
    public object? GetService( Type serviceType )
        => serviceType == typeof(IGreetingService) ? new GreetingService() : null;
}

internal class ConfigureDependencyInjectionFabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
        => amender.ConfigureDependencyInjection(
            builder => builder.RegisterFramework<ServiceLocatorDependencyInjectionFramework>( 1 ) );
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
        ServiceProviderProvider.ServiceProvider = () => new SimpleServiceProvider();

        // No constructor parameter: the dependency comes from the service locator.
        var consumer = new Consumer();

        PackageTestCheck.That( consumer.SayHello() == "hello", "the dependency is resolved through the service locator" );

        return PackageTestCheck.Report( "Metalama.Extensions.DependencyInjection.ServiceLocator" );
    }
}
