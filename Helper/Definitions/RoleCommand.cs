using Discord;
using Discord.WebSocket;

namespace Helper.Definitions; 
using SlashHandler = Func<SocketSlashCommand, DiscordSocketClient, bool>;
public class RoleCommand : Command {

  private new static readonly SlashHandler Handler = (arg, _) => {
    IRole role = (IRole)arg.Data.Options.First().Value;
    Config.SetGuildRole(role.Guild.Id, role.Id);
    arg.RespondAsync(text: $"Counting role set to {role.Name}!", ephemeral: true);
    return true;
  };

  private static readonly List<SlashCommandOptionBuilder> OptionBuilder = new List<SlashCommandOptionBuilder>() {
    new SlashCommandOptionBuilder()
      .WithName("role")
      .WithDescription("The role to set")
      .WithRequired(true)
      .WithType(ApplicationCommandOptionType.Role),
  };
  
  internal RoleCommand() : base("role", "Set the counting role", OptionBuilder, Handler) {}
}