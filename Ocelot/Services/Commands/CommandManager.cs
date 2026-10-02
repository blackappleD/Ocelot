using System.Text.RegularExpressions;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using Ocelot.Extensions;
using Ocelot.Lifecycle;
using Ocelot.Services.Logger;
using Ocelot.Services.Translation;

namespace Ocelot.Services.Commands;

public class CommandManager : IOnStart, IDisposable
{
    private readonly ICommandManager manager;

    private readonly IEnumerable<IOcelotCommand> commands;

    private readonly ITranslator translator;

    private readonly IChatGui chat;

    private readonly static Regex ArgPattern = new(@"[\""].+?[\""]|[^ ]+", RegexOptions.Compiled);

    private readonly ILogger logger;

    private bool firstTime = true;

    public CommandManager(IEnumerable<IOcelotCommand> commands, ICommandManager manager, ITranslator translator, IChatGui chat, ILogger<CommandManager> logger)
    {
        this.manager = manager;
        this.commands = commands;
        this.translator = translator;
        this.chat = chat;
        this.logger = logger;

        translator.TranslationsChanged += ReloadCommands;
        translator.LanguageChanged += ReloadCommands;
    }

    public void OnStart()
    {
        ReloadCommands();
    }

    private string GetCommand(string trigger)
    {
        return !trigger.StartsWith("/") ? trigger.WithPrefix("/") : trigger;
    }

    private void ReloadCommands()
    {
        if (!firstTime)
        {
            foreach (var cmd in commands)
            {
                manager.RemoveHandler(GetCommand(cmd.Command));
                foreach (var alias in cmd.Aliases)
                {
                    manager.RemoveHandler(GetCommand(alias));
                }
            }
        }

        foreach (var cmd in commands.OrderBy(c => c.DisplayOrder))
        {
            if (!cmd.IsEnabled)
            {
                continue;
            }

            Register(cmd);
        }

        firstTime = false;
    }

    private void Register(IOcelotCommand cmd)
    {
        var command = GetCommand(cmd.Command);
        logger.Debug("Registering {c} command", command);

        var (helpMsg, showInHelp) = cmd.BuildHelp();

        var info = new CommandInfo((_, rawArgs) =>
        {
            try
            {
                var args = ParseArgs(rawArgs);
                if (!cmd.ValidateArguments(args, out var error))
                {
                    if (!string.IsNullOrEmpty(error))
                    {
                        chat.PrintError(error);
                    }

                    return;
                }

                var ctx = new CommandContext
                {
                    RawArgs = rawArgs,
                    Args = args,
                };

                cmd.Execute(ctx);
            }
            catch (Exception ex)
            {
                chat.PrintError($"Command error: {ex.Message}");
            }
        })
        {
            HelpMessage = helpMsg,
            ShowInHelp = showInHelp && !cmd.Hidden,
            DisplayOrder = cmd.DisplayOrder,
        };

        manager.AddHandler(command, info);

        foreach (var alias in cmd.Aliases)
        {
            var aliasInfo = new CommandInfo((_, rawArgs) =>
            {
                try
                {
                    var args = ParseArgs(rawArgs);
                    if (!cmd.ValidateArguments(args, out var error))
                    {
                        if (!string.IsNullOrEmpty(error))
                        {
                            chat.PrintError(error);
                        }

                        return;
                    }

                    var ctx = new CommandContext
                    {
                        RawArgs = rawArgs,
                        Args = args,
                    };

                    cmd.Execute(ctx);
                }
                catch (Exception ex)
                {
                    chat.PrintError($"Command error: {ex.Message}");
                }
            })
            {
                ShowInHelp = false,
            };

            manager.AddHandler(GetCommand(alias), aliasInfo);
        }
    }

    private static string[] ParseArgs(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return ArgPattern.Matches(raw)
            .Select(m => m.Value.Trim().Trim('"'))
            .Where(s => s.Length > 0)
            .ToArray();
    }

    public void Dispose()
    {
        translator.LanguageChanged -= ReloadCommands;
        translator.TranslationsChanged -= ReloadCommands;

        foreach (var cmd in commands)
        {
            try
            {
                manager.RemoveHandler(GetCommand(cmd.Command));
                foreach (var alias in cmd.Aliases)
                {
                    manager.RemoveHandler(GetCommand(alias));
                }
            }
            catch
            {
            }
        }
    }
}
