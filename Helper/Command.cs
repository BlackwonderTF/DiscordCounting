using Discord.WebSocket;

namespace Helper;
using Discord;
using SlashHandler = Func<SocketSlashCommand, DiscordSocketClient, bool>;
using UserHandler = Func<SocketUserCommand, DiscordSocketClient, bool>;

public abstract class Command {
  internal string Name { get; }

  private string Description { get; }
  List<SlashCommandOptionBuilder>? Options { get; }

  internal SlashHandler? SlashHandler { get; }
  internal UserHandler? UserHandler { get; }
  
  public bool IsSlashCommand => SlashHandler is not null;
  public bool IsUserCommand => UserHandler is not null;

  internal Command(string name, string description, List<SlashCommandOptionBuilder>? options = null, SlashHandler? slashHandler = null, UserHandler? userHandler = null) {
    Name = name.Length > 0 ? string.Concat(name[0].ToString().ToLower(), name.AsSpan(1)) : name;
    Description = description;
    Options = options;
    SlashHandler = slashHandler;
    UserHandler = userHandler;
  }
  
  public UserCommandBuilder BuildUserCommand() {
    UserCommandBuilder builder = new UserCommandBuilder()
      .WithName(Name)
      .WithDefaultPermission(false)
      .WithDMPermission(false);

    return builder;
  }

  public SlashCommandBuilder BuildSlashCommand() {
    SlashCommandBuilder builder = new SlashCommandBuilder()
      .WithName(Name)
      .WithDescription(Description)
      .WithDefaultPermission(false)
      .WithDMPermission(false);

    if (Options is not null) {
      builder = builder.AddOptions(Options.ToArray());
    }
    
    return builder;
  }
}
