namespace Ocelot.Services.WindowManager;

public interface IMainRenderer
{
    void Render();
}

public class NullMainRenderer : IMainRenderer
{
    public void Render()
    {
    }
}

public interface IConfigRenderer
{
    void Render();
}

public class NullConfigRenderer : IConfigRenderer
{
    public void Render()
    {
    }
}
