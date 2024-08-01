using Discord;
using Discord.WebSocket;

namespace Helper.Definitions; 
using SlashHandler = Func<SocketSlashCommand, DiscordSocketClient, bool>;
using UserHandler = Func<SocketUserCommand, DiscordSocketClient, bool>;

public class InnumerateCommand : Command {

  private enum InnumerateChoice {
    Add = 1,
    Remove = 2,
  }
  
  private new static readonly UserHandler UserHandler = (arg, client) => {
    var target = (SocketGuildUser)arg.Data.Member;
    
    var config = Config.GetGuildConfig(target.Guild.Id);
    var roleId = config.RoleId;
    
    if (roleId is null) {
      return false;
    }
    
    if (target.Roles.Select(r => r.Id).Contains(roleId.Value)) {
      Config.RemoveInnumerate(target.Guild.Id, target);
      arg.RespondAsync(text: $"Removed {target.Mention} from the innumerate list", ephemeral: true);
    } else {
      Config.AddInnumerate(target.Guild.Id, target);
      arg.RespondAsync(text: $"Added {target.Mention} to the innumerate list", ephemeral: true);
    }
    
    return true;
  };
  
  private new static readonly SlashHandler SlashHandler = (arg, client) => {
    var guildId = arg.GuildId;
    if (guildId is null) {
      return false;
    }
    
    var guild = client.Guilds.FirstOrDefault(x => x.Id == guildId);
    
    if (guild is null) {
      return false;
    }

    var options = arg.Data.Options.First();

    if (options is null) {
      return false;
    }
    
    var fieldName = options.Name;
    Func<string, Task> respond = str => arg.RespondAsync(text: str, ephemeral: true);

    return fieldName switch {
      User => HandleInnumerate(options.Options, respond, guild, client),
      Update => HandleInnumerateList(respond, guild).GetAwaiter().GetResult(),
      _ => false,
    };
  };

  private static async Task<bool> HandleInnumerateList(Func<string, Task> respond, SocketGuild guild) {
    IEnumerable<IGuildUser>? users = await guild.GetUsersAsync().FlattenAsync();
    
    var config = Config.GetGuildConfig(guild.Id);
    var roleId = config.RoleId;
    
    if (roleId is null) {
      return false;
    }

    var configInnumerates = config.Innumerates;

    IEnumerable<IGuildUser> innumerates = users.Where(u => u.RoleIds.Contains(roleId.Value));
    foreach (var guildUser in innumerates) {
      config.Innumerates.Add(guildUser.Id);
    }

    Config.UpdateInnumerates(guild.Id, configInnumerates);
    
    await respond("Updated innumerates");
    
    return true;
  }

  private static bool HandleInnumerate(IEnumerable<SocketSlashCommandDataOption> options, Func<string, Task> respond, SocketGuild guild, DiscordSocketClient discordSocketClient) {
    Dictionary<string, SocketSlashCommandDataOption> args = options.ToDictionary(x => x.Name, x => x);
    
    var choice = (InnumerateChoice)Convert.ToInt32((long)args[Choice].Value);
    var target = (SocketGuildUser)args[Who].Value;

    switch (choice) {
      case InnumerateChoice.Add:
        Config.AddInnumerate(guild.Id, target);
        respond($"{target.Mention} has been made innumerate");
        break;
      case InnumerateChoice.Remove:
        Config.RemoveInnumerate(guild.Id, target);
        respond($"{target.Mention} has been allowed to count again");
        break;
      default:
        return false;
    }
    return true;
  }

  private const string User = "user";
  private const string Update = "update";
  private const string Who = "who";
  private const string Choice = "choice";
  
  private static readonly List<SlashCommandOptionBuilder> OptionBuilder =
  [
    new SlashCommandOptionBuilder()
      .WithName(User)
      .WithDescription("User updating")
      .WithType(ApplicationCommandOptionType.SubCommand)
      .AddOptions([
          new SlashCommandOptionBuilder()
            .WithName(Choice)
            .WithDescription("Whether to add or remove the role")
            .AddChoice("add", (int)InnumerateChoice.Add)
            .AddChoice("remove", (int)InnumerateChoice.Remove)
            .WithRequired(true)
            .WithType(ApplicationCommandOptionType.Integer),
          new SlashCommandOptionBuilder()
            .WithName(Who)
            .WithDescription("The person or people to remove the role from")
            .WithRequired(true)
            .WithType(ApplicationCommandOptionType.User)
        ]
      ),

    new SlashCommandOptionBuilder()
      .WithName(Update)
      .WithDescription("Update the innumerate list")
      .WithType(ApplicationCommandOptionType.SubCommand)

  ];
  
  internal InnumerateCommand() : base("innumerate", "Modifies the innumerate list", OptionBuilder, slashHandler: SlashHandler, userHandler: UserHandler) {}
}
