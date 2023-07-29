using Discord.WebSocket;

namespace Helper;
using Discord;
using SlashHandler = Func<SocketSlashCommand, DiscordSocketClient, bool>;

public abstract class Command {
  internal string Name { get; }

  private string Description { get; }
  List<SlashCommandOptionBuilder>? Options { get; }

  internal SlashHandler Handler { get; }

  protected Command(string name, string description, SlashHandler handler) : this(name, description, null, handler) { }

  protected Command(string name, string description, List<SlashCommandOptionBuilder>? options, SlashHandler handler) {
    Name = name.Length > 0 ? string.Concat(name[0].ToString().ToLower(), name.AsSpan(1)) : name;
    Description = description;
    Options = options;
    Handler = handler;
  }

  public SlashCommandBuilder Build() {
    SlashCommandBuilder? builder = new SlashCommandBuilder()
      .WithName(Name)
      .WithDescription(Description);

    if (Options is not null) {
      builder = builder.AddOptions(Options.ToArray());
    }
    
    return builder;
  }
}