namespace Ocelot.Config.Renderers.Enum;

public interface IEnumDisplay<in TEnum>
    where TEnum : struct, System.Enum
{
    string Display(TEnum value);
}

public class GenericDisplay<TEnum> : IEnumDisplay<TEnum>
    where TEnum : struct, System.Enum
{
    public string Display(TEnum value)
    {
        return value.ToString();
    }
}

public interface IEnumFilter<in TEnum>
    where TEnum : struct, System.Enum
{
    bool Filter(TEnum value);
}

public class NoOpFilter<TEnum> : IEnumFilter<TEnum>
    where TEnum : struct, System.Enum
{
    public bool Filter(TEnum value)
    {
        return true;
    }
}
