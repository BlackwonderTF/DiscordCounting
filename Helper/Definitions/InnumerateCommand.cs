using Discord;
using Discord.WebSocket;

namespace Helper.Definitions; 
using SlashHandler = Func<SocketSlashCommand, DiscordSocketClient, bool>;
public class InnumerateCommand : Command {

  private enum InnumerateChoice {
    Add = 1,
    Remove = 2,
  }
  
  private new static readonly SlashHandler Handler = (arg, client) => {
    ulong? guildId = arg.GuildId;
    if (guildId is null) {
      return false;
    }
    
    SocketGuild? guild = client.Guilds.FirstOrDefault(x => x.Id == guildId);
    
    if (guild is null) {
      return false;
    }

    SocketSlashCommandDataOption? options = arg.Data.Options.First();

    if (options is null) {
      return false;
    }
    
    string fieldName = options.Name;
    Func<string, Task> respond = str => arg.RespondAsync(text: str, ephemeral: true);

    return fieldName switch {
      User => HandleInnumerate(options.Options, respond, guild, client),
      Update => HandleInnumerateList(respond, guild, client).GetAwaiter().GetResult(),
      _ => false,
    };

    
  };

  private static async Task<bool> HandleInnumerateList(Func<string, Task> respond, SocketGuild guild, BaseSocketClient client) {
    IEnumerable<IGuildUser>? users = await guild.GetUsersAsync().FlattenAsync();
    
    GuildConfig config = Config.GetGuildConfig(guild.Id);
    ulong? roleId = config.RoleId;
    
    if (roleId is null) {
      return false;
    }

    HashSet<ulong> configInnumerates = config.Innumerates;

    IEnumerable<IGuildUser> innumerates = users.Where(u => u.RoleIds.Contains(roleId.Value));
    foreach (IGuildUser guildUser in innumerates) {
      config.Innumerates.Add(guildUser.Id);
    }

    Config.UpdateInnumerates(guild.Id, configInnumerates);
    
    await respond("Updated innumerates");
    
    return true;
  }

  private static bool HandleInnumerate(IEnumerable<SocketSlashCommandDataOption> options, Func<string, Task> respond, SocketGuild guild, DiscordSocketClient discordSocketClient) {
    Dictionary<string, SocketSlashCommandDataOption> args = options.ToDictionary(x => x.Name, x => x);
    
    InnumerateChoice choice = (InnumerateChoice)Convert.ToInt32((long)args[Choice].Value);
    SocketGuildUser target = (SocketGuildUser)args[Who].Value;

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
  
  private static readonly List<SlashCommandOptionBuilder> OptionBuilder = new List<SlashCommandOptionBuilder>() {
    new SlashCommandOptionBuilder()
      .WithName(User)
      .WithDescription("User updating")
      .WithType(ApplicationCommandOptionType.SubCommand)
      .AddOptions(new [] {
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
            .WithType(ApplicationCommandOptionType.User),
        }
      ),
    new SlashCommandOptionBuilder()
      .WithName(Update)
      .WithDescription("Update the innumerate list")
      .WithType(ApplicationCommandOptionType.SubCommand),
  };
  
  internal InnumerateCommand() : base("innumerate", "Modifies the innumerate list", OptionBuilder, Handler) {}
}
