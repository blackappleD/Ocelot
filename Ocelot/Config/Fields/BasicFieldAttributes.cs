using Ocelot.Config.Renderers;

namespace Ocelot.Config.Fields;

public class CheckboxAttribute() : UIFieldAttribute(typeof(CheckboxRenderer));

public class StringInputAttribute() : UIFieldAttribute(typeof(StringInputRenderer))
{
    public int MaxLength { get; set; } = 256;
}

public sealed class IntRangeAttribute(int min, int max) : UIFieldAttribute(typeof(IntRangeRenderer))
{
    public int Min { get; } = min;

    public int Max { get; } = max;
}

public sealed class FloatRangeAttribute(float min, float max) : UIFieldAttribute(typeof(FloatRangeRenderer))
{
    public float Min { get; } = min;

    public float Max { get; } = max;
}
