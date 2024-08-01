using System.Text;
using System.Text.Json;
using Discord;
using Discord.WebSocket;

namespace Helper;

public static class Config {

  private static DiscordSocketClient? _client;

  public static Dictionary<ulong, GuildConfig> Guilds { get; private set; } = new();

  public static string Serialize() {
    return JsonSerializer.Serialize(Guilds);
  }

  public static void DeSerialize(string json) {
    Guilds = JsonSerializer.Deserialize<Dictionary<ulong, GuildConfig>>(json) ?? new Dictionary<ulong, GuildConfig>();
    // Guilds = JsonSerializer.Deserialize<List<KeyValuePair<ulong, GuildConfig>>>(json)?.ToDictionary(x => x.Key, x => x.Value) ?? new Dictionary<ulong, GuildConfig>();
    Console.WriteLine(Guilds);
  }

  public static void SetClient(DiscordSocketClient client) {
    _client = client;
  }

  public static async Task SetUpGuild(GuildConfig guild) {
    if (_client is null) {
      await Console.Error.WriteLineAsync("Client is not set up!");
      Environment.Exit(-1);
      return;
    }

    Console.WriteLine(guild.ToString());

    if (guild.ChannelId is null) return;

    var channel = (SocketTextChannel)_client.GetChannel(guild.ChannelId.Value);

    if (channel is null) return;

    IAsyncEnumerable<IReadOnlyCollection<IMessage>> res = channel.GetMessagesAsync(limit: 20);

    await foreach (var x in res) {
      var message = x.FirstOrDefault(message => {
        var firstWord = message.CleanContent.Split(' ');
        return firstWord.Length != 0 && ulong.TryParse(firstWord[0], out var _);
      });

      if (message is null) {
        continue;
      }

      var guildConfig = guild;
      guildConfig.Count = ulong.Parse(message.CleanContent.Split(' ')[0]);
      guildConfig.LastAuthorId = message.Author.Id;
      SetGuildConfig(channel.Guild.Id, guildConfig);
      break;
    }
  }

  public static async void SetGuildChannel(ulong guildId, ulong channelId) {
    var guildConfig = GetGuildConfig(guildId);
    guildConfig.ChannelId = channelId;
    SetGuildConfig(guildId, guildConfig);
    await SetUpGuild(guildConfig);
  }

  public static void SetGuildRole(ulong guildId, ulong roleId) {
    var guildConfig = GetGuildConfig(guildId);
    guildConfig.RoleId = roleId;
    SetGuildConfig(guildId, guildConfig);
  }

  public static GuildConfig GetGuildConfig(ulong id) {
    Guilds.TryAdd(id, new GuildConfig());
    return Guilds[id];
  }

  public static void SetGuildConfig(ulong id, GuildConfig guildConfig) {
    if (!Guilds.TryAdd(id, guildConfig)) {
      Guilds[id] = guildConfig;
    }
  }

  public static ulong SetCount(ulong guildId, GuildConfig guildConfig, ulong count) {
    guildConfig.Count = count;
    SetGuildConfig(guildId, guildConfig);
    return count;
  }

  public static void SetLeniency(ulong guildId, uint leniency) {
    var guildConfig = GetGuildConfig(guildId);
    guildConfig.Leniency = leniency;
    SetGuildConfig(guildId, guildConfig);
  }

  static void SetLastAuthorId(ulong guildId, ulong lastAuthorId) {
    var guildConfig = GetGuildConfig(guildId);
    guildConfig.LastAuthorId = lastAuthorId;
    SetGuildConfig(guildId, guildConfig);
  }

  public static ulong GetCount(ulong guildId) {
    var guildConfig = GetGuildConfig(guildId);

    guildConfig.Count ??= SetCount(guildId, guildConfig, 0);

    return guildConfig.Count.Value;
  }

  public static uint GetLeniency(ulong guildId) {
    var guildConfig = GetGuildConfig(guildId);
    return guildConfig.Leniency;
  }

  // public static bool IsSetUp(this GuildConfig guildConfig) {
  //   return guildConfig is {ChannelId: not null, RoleId: not null};
  // }

  public static void SetEnabled(ulong guildId, bool enabled) {
    var guildConfig = GetGuildConfig(guildId);
    guildConfig.IsEnabled = enabled;
    SetGuildConfig(guildId, guildConfig);
  }

  public static bool GetEnabled(ulong guildId) {
    var guildConfig = GetGuildConfig(guildId);
    return guildConfig.IsEnabled;
  }
  
  public static void SetEditResets(ulong guildId, bool editResets) {
    var guildConfig = GetGuildConfig(guildId);
    guildConfig.EditResets = editResets;
    SetGuildConfig(guildId, guildConfig);
  }
  
  public static bool GetEditResets(ulong guildId) {
    var guildConfig = GetGuildConfig(guildId);
    return guildConfig.EditResets;
  }
  
  public static void SetDeleteResets(ulong guildId, bool deleteResets) {
    var guildConfig = GetGuildConfig(guildId);
    guildConfig.DeleteResets = deleteResets;
    SetGuildConfig(guildId, guildConfig);
  }
  
  public static bool GetDeleteResets(ulong guildId) {
    var guildConfig = GetGuildConfig(guildId);
    return guildConfig.DeleteResets;
  }
  
  public static void AddInnumerate(ulong guildId, SocketGuildUser target, bool addRole = true) {
    var guildConfig = GetGuildConfig(guildId);
    
    if (guildConfig.RoleId is not null && addRole) {
      target.AddRoleAsync(guildConfig.RoleId.Value);
    }
    
    guildConfig.Innumerates.Add(target.Id);
    SetGuildConfig(guildId, guildConfig);
  }
  
  public static void AddInnumerate(ulong guildId, ulong target) {
    var guildConfig = GetGuildConfig(guildId);
    guildConfig.Innumerates.Add(target);
    SetGuildConfig(guildId, guildConfig);
  }
  
  public static void RemoveInnumerate(ulong guildId, SocketGuildUser target, bool removeRole = true) {
    var guildConfig = GetGuildConfig(guildId);
    
    if (guildConfig.RoleId is not null && removeRole) {
      target.RemoveRoleAsync(guildConfig.RoleId.Value);
    }
    
    guildConfig.Innumerates.Remove(target.Id);
    SetGuildConfig(guildId, guildConfig);
  }
  
  public static void RemoveInnumerate(ulong guildId, ulong target) {
    var guildConfig = GetGuildConfig(guildId);
    guildConfig.Innumerates.Remove(target);
    SetGuildConfig(guildId, guildConfig);
  }
  
  public static bool IsInnumerate(ulong guildId, ulong innumerate) {
    var guildConfig = GetGuildConfig(guildId);
    return guildConfig.Innumerates.Contains(innumerate);
  }

  public static void UpdateInnumerates(ulong guildId, HashSet<ulong> configInnumerates) {
    var guildConfig = GetGuildConfig(guildId);
    guildConfig.Innumerates = configInnumerates;
    SetGuildConfig(guildId, guildConfig);
  }

  public static void AddSecondaryRole(ulong guildId, ulong newUserId, ulong roleId) {
    var guildConfig = GetGuildConfig(guildId);
    if (!guildConfig.SecondaryRolesStorage.ContainsKey(newUserId)) {
      guildConfig.SecondaryRolesStorage.Add(newUserId, []);
    }
    guildConfig.SecondaryRolesStorage[newUserId].Add(roleId);
    SetGuildConfig(guildId, guildConfig);
  }
  
  public static void RemoveSecondaryRole(ulong guildId, ulong newUserId, ulong roleId) {
    var guildConfig = GetGuildConfig(guildId);
    if (!guildConfig.SecondaryRolesStorage.ContainsKey(newUserId)) {
      return;
    }
    guildConfig.SecondaryRolesStorage[newUserId].Remove(roleId);
    SetGuildConfig(guildId, guildConfig);
  }

  public static bool ToggleSecondaryRoleId(ulong guildId, ulong roleId) { 
    var guildConfig = GetGuildConfig(guildId);

    var returnValue = false;
    if (guildConfig.SecondaryRoles.Contains(roleId)) {
      guildConfig.SecondaryRoles.Remove(roleId);
    } else {
      guildConfig.SecondaryRoles.Add(roleId);
      returnValue = true;
    }
    SetGuildConfig(guildId, guildConfig);
    return returnValue;
  }
}

public struct GuildConfig {
  public GuildConfig() {
  }
  
  public bool IsSetUp => ChannelId is not null && RoleId is not null;

  public ulong? ChannelId { get; set; } = null;
  public ulong? RoleId { get; set; } = null;
  public ulong? Count { get; set; } = null;
  public ulong? LastAuthorId { get; set; } = null;
  public uint Leniency { get; set; } = 0;
  public bool IsEnabled { get; set; } = true;
  public bool EditResets { get; set; } = true;
  public bool DeleteResets { get; set; } = true;
  public HashSet<ulong> Innumerates { get; set; } = [];
  public HashSet<ulong> SecondaryRoles { get; set; } = [];
  public Dictionary<ulong, HashSet<ulong>> SecondaryRolesStorage { get; set; } = new Dictionary<ulong, HashSet<ulong>>();
  

  public override string ToString() {
    var sb = new StringBuilder();
    sb.Append("{\n");
    sb.Append($"\tChannel: <#{ChannelId}>\n");
    sb.Append($"\tRole: <@&{RoleId}>\n");
    sb.Append($"\tLastAuthor: <@{LastAuthorId}>\n");
    sb.Append($"\tCurrent Count: {Count}\n");
    sb.Append($"\tLeniency: {Leniency}\n");
    sb.Append($"\tEditResets: {EditResets}\n");
    sb.Append($"\tDeleteResets: {DeleteResets}\n");
    sb.Append($"\tIsEnabled: {IsEnabled}\n");
    sb.Append($"\tIsSetUp: {IsSetUp}\n");
    sb.Append($"\tInnumerates: {Innumerates.Count}\n");
    sb.Append('}');
    return sb.ToString();
  }

}
