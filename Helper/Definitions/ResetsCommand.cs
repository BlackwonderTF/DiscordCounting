﻿using Discord;
using Discord.WebSocket;

namespace Helper.Definitions;

using SlashHandler = Func<SocketSlashCommand, DiscordSocketClient, bool>;

public class ResetsCommand : Command {

  private static readonly SlashHandler Handler = (command, _) => {
    var guildId = command.GuildId;

    if (guildId is null) {
      return false;
    }

    var options = command.Data.Options.First();
    Dictionary<string, SocketSlashCommandDataOption> args = options.Options.First().Options.ToDictionary(x => x.Name, x => x);

    var edit = (bool)args[Edit].Value;
    // bool delete = (bool)args[Delete].Value;
    
    Config.SetEditResets(guildId.Value, edit);
    // Config.SetDeleteResets(guildId.Value, delete);

    return true;
  };
  
  private const string Edit = "edit";
  // private const string Delete = "delete";

  private static readonly List<SlashCommandOptionBuilder> OptionBuilder =
  [
    new SlashCommandOptionBuilder()
      .WithName(Edit)
      .WithDescription("Edited messages")
      .WithRequired(true)
      .WithType(ApplicationCommandOptionType.Boolean)
    // new SlashCommandOptionBuilder()
    //   .WithName("delete")
    //   .WithDescription("Deleted messages")
    //   .WithRequired(true)
    //   .WithType(ApplicationCommandOptionType.Boolean),

  ];

  internal ResetsCommand() : base("resets", "Sets what resets the count.", OptionBuilder, Handler) {
  }
}
