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
  
  private static Command? GetCommand(string name) {
    return Commands.TryGetValue(name, out Command? command) ? command : null;
  }

  private static void ExecuteCommand(string commandName, Func<Command, bool> callBack, Func<string, bool, Task> errorCallBack) {
    if (GetCommand(commandName) is not { } command) {
      return;
    }
    
    try {
      bool res = callBack(command);

      if (!res) {
        errorCallBack("Something went wrong executing the command!", true);
      }
    } catch (Exception _) {
      errorCallBack("Something went wrong executing the command!", true);
    }
  }

  public static void ExecuteCommand(SocketUserCommand userCommand, DiscordSocketClient discordSocketClient) {
    ExecuteCommand(
      userCommand.CommandName, 
      command => command.UserHandler is not null && command.UserHandler(userCommand, discordSocketClient), 
      (error, ephemeral) => userCommand.RespondAsync(error, ephemeral: ephemeral)
    );
  }
  
  public static void ExecuteCommand(SocketSlashCommand socketSlashCommand, DiscordSocketClient discordSocketClient) {
    ExecuteCommand(
      socketSlashCommand.CommandName, 
      command => command.SlashHandler is not null && command.SlashHandler(socketSlashCommand, discordSocketClient), 
      (error, ephemeral) => socketSlashCommand.RespondAsync(error, ephemeral: ephemeral)
    );
  }
}