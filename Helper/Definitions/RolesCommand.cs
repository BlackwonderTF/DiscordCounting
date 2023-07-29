using Discord;
using Discord.WebSocket;

namespace Helper.Definitions; 
using SlashHandler = Func<SocketSlashCommand, DiscordSocketClient, bool>;
public class RolesCommand : Command {

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
      RoleCategory => HandleRole(options.Options, respond, guild, client),
      CurrentRoles => HandleCurrentRoles(respond, guild, client).GetAwaiter().GetResult(),
      Update => HandleUpdate(respond, guild, client).GetAwaiter().GetResult(),
      _ => false,
    };
  };

  private static async Task<bool> HandleUpdate(Func<string,Task> respond, SocketGuild guild, DiscordSocketClient client) {
    GuildConfig config = Config.GetGuildConfig(guild.Id);
    
    // Get all secondary roles
    List<SocketRole> secondaryRoles = config.SecondaryRoles
      .Select(guild.GetRole)
      .ToList();
    
    // Get all users with the roles in a dictionary
    Dictionary<ulong, HashSet<ulong>> dictionary = new Dictionary<ulong, HashSet<ulong>>();
    IEnumerable<IGuildUser>? roleUsers = await guild.GetUsersAsync().FlattenAsync();
    List<IGuildUser> guildUsers = roleUsers.ToList();

    try {
      foreach (IRole role in secondaryRoles) {
        List<IGuildUser> users = guildUsers.Where(u => u.RoleIds.Contains(role.Id)).ToList();
        foreach (IGuildUser user in users) {
          if (dictionary.TryGetValue(role.Id, out HashSet<ulong>? foundUsers)) {
            foundUsers.Add(user.Id);
          } else {
            dictionary.TryAdd(role.Id, new HashSet<ulong>() {user.Id});
          }
        }
      }
    } catch (Exception e) {
      Console.WriteLine(e);
      throw;
    }
    
    // Update the config
    config.SecondaryRolesStorage = config.SecondaryRolesStorage
      .Union(dictionary)
      .GroupBy(g => g.Key)
      .ToDictionary(
        pair => pair.Key, 
        pair => pair
          .Select(x => x.Value)
          .Aggregate(
            (a, b) => a
              .Union(b)
              .ToHashSet()
          )
      );
    
    Config.SetGuildConfig(guild.Id, config);
    await respond("Updated the config!");

    return true;
  }

  private static bool HandleRole(IEnumerable<SocketSlashCommandDataOption> optionsOptions, Func<string,Task> respond, SocketGuild guild, DiscordSocketClient client) {
    SocketSlashCommandDataOption? roleOption = optionsOptions.FirstOrDefault(x => x.Name == Role);
    if (roleOption is null) {
      return false;
    }

    IRole role = (IRole)roleOption.Value;
    bool value = Config.ToggleSecondaryRoleId(guild.Id, role.Id);
    string res = value ? "added" : "removed";
    respond($"Secondary role {res}: {role.Name}!");
    return true;
  }
  
  private static async Task<bool> HandleCurrentRoles(Func<string, Task> respond, SocketGuild guild, DiscordSocketClient client) {
    GuildConfig config = Config.GetGuildConfig(guild.Id);
    await respond($$"""
      The current roles are:
      {{
        string.Join("\n", config.SecondaryRoles.Select(x => $"<@&{x}>"))
      }}
    """);
    
    return true;
  }

  private const string RoleCategory = "role";
  private const string Role = "role";
  private const string CurrentRoles = "current_roles";
  private const string Update = "update";
  
  private static readonly List<SlashCommandOptionBuilder> OptionBuilder = new List<SlashCommandOptionBuilder>() {
    new SlashCommandOptionBuilder()
      .WithName(RoleCategory)
      .WithDescription("The role to add or remove")
      .WithType(ApplicationCommandOptionType.SubCommand)
      .AddOption(new SlashCommandOptionBuilder()
        .WithName(Role)
        .WithDescription("The role to add or remove")
        .WithRequired(true)
        .WithType(ApplicationCommandOptionType.Role)
      ),
    new SlashCommandOptionBuilder()
      .WithName(CurrentRoles)
      .WithDescription("A list of all the current roles")
      .WithType(ApplicationCommandOptionType.SubCommand),
    new SlashCommandOptionBuilder()
      .WithName(Update)
      .WithDescription("Update the secondary list")
      .WithType(ApplicationCommandOptionType.SubCommand),
  };
  
  internal RolesCommand() : base("roles", "Modifies the secondary roles list", OptionBuilder, Handler) {}
}
