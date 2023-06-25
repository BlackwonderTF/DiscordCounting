using Discord.WebSocket;
using Helper.Definitions;
using SlashHandler = System.Func<Discord.WebSocket.SocketSlashCommand, bool>;

namespace Helper; 

public static class CommandStorage {
  public static readonly List<KeyValuePair<string, Command>> Commands = new List<Command>() {
    new PingCommand(),
    new ChannelCommand(),
    new RoleCommand(),
    new LeniencyCommand(),
    new EnableCommand(),
  }.Select(cmd => new KeyValuePair<string, Command>(cmd.Name, cmd)).ToList();
  
  public static void ExecuteCommand(SocketSlashCommand socketSlashCommand) {
    try {
      // Find the command
      KeyValuePair<string, Command> command = Commands.First(cmd => cmd.Key == socketSlashCommand.Data.Name);

      // Execute the command
      bool res = command.Value.Handler(socketSlashCommand);
      if (!res) {
        socketSlashCommand.RespondAsync("Something went wrong executing the command!", ephemeral: true);
      }
    } catch (Exception _) {
      // ignored
    }
  }
}