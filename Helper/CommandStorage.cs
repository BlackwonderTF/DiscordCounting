using Discord.WebSocket;
using Helper.Definitions;
using SlashHandler = System.Func<Discord.WebSocket.SocketSlashCommand, bool>;

namespace Helper; 

public static class CommandStorage {
  public static readonly Dictionary<string, Command> Commands = new Dictionary<string, Command>(new List<Command>() {
    new PingCommand(),
    new ChannelCommand(),
    new RoleCommand(),
    new LeniencyCommand(),
    new EnableCommand(),
    new ConfigCommand(),
    new ResetsCommand(),
    new InnumerateCommand(),
    new RolesCommand(),
  }.ToDictionary(cmd => cmd.Name, cmd => cmd));
  
  public static void ExecuteCommand(SocketSlashCommand socketSlashCommand, DiscordSocketClient discordSocketClient) {
    try {
      // Find the command
      if (!Commands.TryGetValue(socketSlashCommand.Data.Name, out Command? command)) {
        return;
      }

      // Execute the command
      bool res = command.Handler(socketSlashCommand, discordSocketClient);
      if (!res) {
        socketSlashCommand.RespondAsync("Something went wrong executing the command!", ephemeral: true);
      }
    } catch (Exception _) {
      // ignored
    }
  }
}