namespace Helper.Definitions;

using SlashHandler = Func<Discord.WebSocket.SocketSlashCommand, bool>;

public class PingCommand : Command {
  
  private new static readonly SlashHandler Handler = (arg) => {
    arg.RespondAsync("pong!");
    return true;
  };
  
  internal PingCommand() : base("ping", "pong", Handler) {}
}
