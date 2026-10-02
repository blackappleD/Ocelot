using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.Services;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class OcelotServiceAttribute : Attribute
{
    public Type? Service { get; init; }

    public ServiceLifetime Lifetime { get; init; } = ServiceLifetime.Singleton;
}

[AttributeUsage(AttributeTargets.Interface)]
public class OcelotAutoWireAttribute : Attribute;
