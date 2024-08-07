﻿using Discord.WebSocket;

namespace Helper.Definitions;

using SlashHandler = Func<SocketSlashCommand, DiscordSocketClient, bool>;

public class PingCommand : Command {
  
  private static readonly SlashHandler Handler = (arg, _) => {
    arg.RespondAsync("pong!");
    return true;
  };
  
  internal PingCommand() : base("ping", "pong", slashHandler: Handler) {}
}
