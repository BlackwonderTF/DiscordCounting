using Discord;
using Discord.WebSocket;

namespace Helper.Definitions; 
using SlashHandler = Func<SocketSlashCommand, DiscordSocketClient, bool>;
public class EnableCommand : Command {

  private new static readonly SlashHandler Handler = (arg, _) => {
    ulong? guildId = arg.GuildId;
    if (guildId is null) {
      return false;
    }
    
    bool enabled = (bool)arg.Data.Options.First().Value;
    Config.SetEnabled(guildId.Value, enabled);
    arg.RespondAsync(text: $"Bot set to {enabled}", ephemeral: true);
    return true;
  };

  private static readonly List<SlashCommandOptionBuilder> OptionBuilder = new List<SlashCommandOptionBuilder>() {
    new SlashCommandOptionBuilder()
      .WithName("enabled")
      .WithDescription("The enabled state")
      .WithRequired(true)
      .WithType(ApplicationCommandOptionType.Boolean),
  };
  
  internal EnableCommand() : base("enabled", "Sets the enabled state", OptionBuilder, Handler) {}
}
