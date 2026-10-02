namespace Ocelot.Config;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ConfigGroupAttribute(string key) : Attribute
{
    public string Key { get; } = key;

    public int GroupOrder { get; init; } = 0;

    public int Order { get; init; } = 0;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = true)]
public sealed class ConfigHiddenAttribute : Attribute;
