using Discord;
using Discord.WebSocket;

namespace Helper.Definitions; 
using SlashHandler = Func<SocketSlashCommand, DiscordSocketClient, bool>;
public class ConfigCommand : Command {

  private static readonly SlashHandler Handler = (arg, _) => {
    var guildId = arg.GuildId;
    if (guildId is null) {
      return false;
    }
    
    var config = Config.GetGuildConfig(guildId.Value);
    arg.RespondAsync(text: $"Current Config:\n {config.ToString()}", ephemeral: true);
    return true;
  };

  private static readonly List<SlashCommandOptionBuilder> OptionBuilder = [];
  
  internal ConfigCommand() : base("config", "Gets the current config", OptionBuilder, Handler) {}
}
