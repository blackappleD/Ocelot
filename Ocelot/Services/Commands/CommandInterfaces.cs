namespace Ocelot.Services.Commands;

public interface IMainCommand : IOcelotCommand;

public interface IConfigCommand : IOcelotCommand;

public interface IMainCommandDelegate
{
    IOcelotCommand Command { get; }
}
