using Discord;
using Discord.WebSocket;

namespace Helper.Definitions; 
using SlashHandler = Func<SocketSlashCommand, DiscordSocketClient, bool>;
public class LeniencyCommand : Command {

  private new static readonly SlashHandler Handler = (arg, _) => {
    ulong? guildId = arg.GuildId;
    if (guildId is null) {
      return false;
    }
    
    long leniency = (long)arg.Data.Options.First().Value;
    Config.SetLeniency(guildId.Value, Convert.ToUInt32(leniency));
    arg.RespondAsync(text: $"Counting leniency set to {leniency}!", ephemeral: true);
    return true;
  };

  private static readonly List<SlashCommandOptionBuilder> OptionBuilder = new List<SlashCommandOptionBuilder>() {
    new SlashCommandOptionBuilder()
      .WithName("leniency")
      .WithDescription("The leniency")
      .WithRequired(true)
      .WithType(ApplicationCommandOptionType.Integer),
  };
  
  internal LeniencyCommand() : base("leniency", "Set the leniency", OptionBuilder, Handler) {}
}
