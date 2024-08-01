using Discord;
using Discord.WebSocket;

namespace Helper.Definitions; 
using SlashHandler = Func<SocketSlashCommand, DiscordSocketClient, bool>;
public class ChannelCommand : Command {

  private static readonly SlashHandler Handler = (arg, _) => {
    var channel = (SocketGuildChannel)arg.Data.Options.First().Value;
    Config.SetGuildChannel(channel.Guild.Id, channel.Id);
    arg.RespondAsync(text: $"Counting channel set to {channel.Name}!", ephemeral: true);
    return true;
  };

  private static readonly List<SlashCommandOptionBuilder> OptionBuilder =
  [
    new SlashCommandOptionBuilder()
      .WithName("channel")
      .WithDescription("The channel to set")
      .WithRequired(true)
      .WithType(ApplicationCommandOptionType.Channel)

  ];
  
  internal ChannelCommand() : base("channel", "Set the counting channel", OptionBuilder, Handler) {}
}
