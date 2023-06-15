using Discord;
using Discord.WebSocket;

namespace Helper.Definitions; 
using SlashHandler = Func<SocketSlashCommand, bool>;
public class ChannelCommand : Command {

  private new static readonly SlashHandler Handler = (arg) => {
    SocketGuildChannel channel = (SocketGuildChannel)arg.Data.Options.First().Value;
    Config.SetGuildChannel(channel.Guild.Id, channel.Id);
    arg.RespondAsync(text: $"Counting channel set to {channel.Name}!", ephemeral: true);
    Console.WriteLine($"New guild config {Config.PrintGuildConfig(Config.GetGuildConfig(channel.Guild.Id))}");
    return true;
  };

  private static readonly List<SlashCommandOptionBuilder> OptionBuilder = new List<SlashCommandOptionBuilder>() {
    new SlashCommandOptionBuilder()
      .WithName("channel")
      .WithDescription("The channel to set")
      .WithRequired(true)
      .WithType(ApplicationCommandOptionType.Channel),
  };
  
  internal ChannelCommand() : base("channel", "Set the counting channel", OptionBuilder, Handler) {}
}
