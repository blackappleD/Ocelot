namespace Ocelot.Services.Commands.MainCommandDelegates;

public class ConfigDelegate(IConfigCommand command) : IMainCommandDelegate
{
    public IOcelotCommand Command { get; } = command;
}

public class ReloadTranslationsDelegate(ReloadTranslationsCommand command) : IMainCommandDelegate
{
    public IOcelotCommand Command { get; } = command;
}
