using Discord;
using Discord.WebSocket;

namespace Helper.Definitions; 
using SlashHandler = Func<SocketSlashCommand, DiscordSocketClient, bool>;
public class ConfigCommand : Command {

  private new static readonly SlashHandler Handler = (arg, _) => {
    ulong? guildId = arg.GuildId;
    if (guildId is null) {
      return false;
    }
    
    GuildConfig config = Config.GetGuildConfig(guildId.Value);
    arg.RespondAsync(text: $"Current Config:\n {config.ToString()}", ephemeral: true);
    return true;
  };

  private static readonly List<SlashCommandOptionBuilder> OptionBuilder = new List<SlashCommandOptionBuilder>();
  
  internal ConfigCommand() : base("config", "Gets the current config", OptionBuilder, Handler) {}
}
