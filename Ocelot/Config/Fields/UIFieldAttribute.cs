namespace Ocelot.Config.Fields;

[AttributeUsage(AttributeTargets.Property)]
public class UIFieldAttribute(Type rendererType) : Attribute
{
    public Type RendererType { get; } = rendererType ?? throw new ArgumentNullException(nameof(rendererType));

    public int Order { get; set; } = 0;

    public int Indent { get; set; } = 0;

    public string? Requires { get; set; }

    public string? DisabledWhen { get; set; }

    public string? Section { get; set; }
}
