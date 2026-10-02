namespace Ocelot.Chain;

public interface IChainFactory
{
    IChain Create(string name);
}

public class ChainFactory(IServiceProvider services) : IChainFactory
{
    public IChain Create(string name)
    {
        return new Chain(name, services);
    }
}
