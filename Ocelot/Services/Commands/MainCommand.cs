using System.Text;
using Dalamud.Plugin.Services;
using Lumina.Extensions;
using Ocelot.Extensions;
using Ocelot.Services.Translation;
using Ocelot.Windows;

namespace Ocelot.Services.Commands;

public class MainCommand : OcelotCommand, IMainCommand
{
    public override string Command { get; }

    private readonly ITranslator translator;

    private readonly IChatGui chat;

    private readonly IMainWindow window;

    private readonly Dictionary<string[], IMainCommandDelegate> delegates = [];

    public MainCommand(
        ITranslator<MainCommand> translator,
        OcelotPlugin plugin,
        IChatGui chat,
        IMainWindow window,
        IEnumerable<IMainCommandDelegate> delegates
    ) : base(translator)
    {
        this.translator = translator;

        var command = plugin.CommandName.ToKebabCase();
        Command = command;

        this.chat = chat;
        this.window = window;

        foreach (var del in delegates)
        {
            var keys = new List<string> { FormatDelegateTrigger(del.Command.Command) };

            foreach (var alias in del.Command.Aliases)
            {
                keys.Add(FormatDelegateTrigger(alias));
            }

            this.delegates.Add(keys.ToArray(), del);
        }
    }

    private string FormatDelegateTrigger(string trigger)
    {
        // "bocchi-config" / "bocchiconfig" → "config" when main command is "bocchi".
        if (trigger.StartsWith($"{Command}-", StringComparison.OrdinalIgnoreCase))
        {
            return trigger[(Command.Length + 1)..];
        }

        if (trigger.StartsWith(Command, StringComparison.OrdinalIgnoreCase)
            && trigger.Length > Command.Length)
        {
            return trigger[Command.Length..].TrimStart('-');
        }

        return trigger;
    }

    public override (string help, bool show) BuildHelp()
    {
        var (help, showHelp) = base.BuildHelp();
        if (!showHelp)
        {
            return (help, showHelp);
        }

        var sb = new StringBuilder(help).AppendLine();

        sb.AppendLine(" subcommands:");

        foreach (var (keys, del) in delegates)
        {
            var joined = string.Join('|', keys);
            sb.Append($"  - {joined}");

            if (translator.Has(del.Command.HelpKey))
            {
                sb.Append($" : {translator.T(del.Command.HelpKey)}");
            }

            sb.AppendLine();
        }

        return (sb.ToString(), showHelp);
    }

    public override void Execute(CommandContext context)
    {
        var args = context.Args;
        if (args.Length == 0)
        {
            window.Toggle();
            return;
        }

        var token = args[0];

        var match = delegates.FirstOrNull(kvp => kvp.Key.Contains(token));
        if (match == null)
        {
            chat.PrintError(translator.T(".argument_not_recognized", ("argument", token)));
            return;
        }

        var del = match.Value.Value;

        var subContext = context.Skip(1);

        if (!del.Command.ValidateArguments(subContext.Args, out var error))
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                chat.PrintError(error);
            }

            return;
        }

        del.Command.Execute(subContext);
    }
}
