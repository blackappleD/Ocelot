using Lumina.Excel;

namespace Ocelot.Config.Renderers.Excel;

public interface IExcelDisplay<in TRow>
    where TRow : struct, IExcelRow<TRow>
{
    string Display(TRow row);
}

public class GenericDisplay<TRow> : IExcelDisplay<TRow>
    where TRow : struct, IExcelRow<TRow>
{
    public string Display(TRow row)
    {
        return row.ToString() ?? row.RowId.ToString();
    }
}

public interface IExcelFilter<in TRow>
    where TRow : struct, IExcelRow<TRow>
{
    bool Filter(TRow row);
}

public class NoOpFilter<TRow> : IExcelFilter<TRow> where TRow : struct, IExcelRow<TRow>
{
    public bool Filter(TRow row)
    {
        return row.RowId > 0;
    }
}
