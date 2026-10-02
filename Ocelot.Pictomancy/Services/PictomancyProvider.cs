using Ocelot.Lifecycle;
using Pictomancy;

namespace Ocelot.Pictomancy.Services;

public class PictomancyProvider : IPictomancyProvider, IOnPreRender, IOnPostRender
{
    private PctDrawList? current;

    public bool HasDrawList
    {
        get => current is not null;
    }

    public PctDrawList GetDrawList()
    {
        if (current is null)
        {
            throw new InvalidOperationException("Pictomancy draw list not available. It is only available during the render lifecycle hook.");
        }

        return current;
    }

    public void PreRender()
    {
        if (current is not null)
        {
            try
            {
                current.Dispose();
            }
            catch
            {
                /* ignore */
            }

            current = null;
        }

        current = PctService.Draw();
    }

    public void PostRender()
    {
        if (current is null)
        {
            return;
        }

        try
        {
            current.Dispose();
        }
        catch (InvalidOperationException)
        {
        }
        finally
        {
            current = null;
        }
    }
}
