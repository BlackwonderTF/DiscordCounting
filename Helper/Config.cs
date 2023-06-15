using System.Text.Json;
using Discord;
using Discord.WebSocket;

namespace Helper; 

public static class Config {
  
  public static List<KeyValuePair<ulong, GuildConfig>> Guilds = new List<KeyValuePair<ulong, GuildConfig>>();
  private static DiscordSocketClient? _client;
  
  public static string Serialize() {
    return JsonSerializer.Serialize(Guilds);
  }

  public static void DeSerialize(string json) {
    Guilds = JsonSerializer.Deserialize<List<KeyValuePair<ulong, GuildConfig>>>(json) ?? new List<KeyValuePair<ulong, GuildConfig>>();
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
    
    Console.WriteLine(Config.PrintGuildConfig(guild));
        
    if (guild.ChannelId is null) return;

    SocketTextChannel? channel = (SocketTextChannel)_client.GetChannel(guild.ChannelId.Value);
        
    if (channel is null) return;

    IAsyncEnumerable<IReadOnlyCollection<IMessage>> res = channel.GetMessagesAsync(limit: 20);

    await foreach (IReadOnlyCollection<IMessage> x in res) {
      IMessage? message = x.FirstOrDefault(message => {
        string[] firstWord = message.CleanContent.Split(' ');
        return firstWord.Length != 0 && ulong.TryParse(firstWord[0], out ulong _);
      });

      if (message is null) {
        continue;
      }

      GuildConfig guildConfig = guild;
      guildConfig.Count = ulong.Parse(message.CleanContent.Split(' ')[0]);
      guildConfig.LastAuthorId = message.Author.Id;
      SetGuildConfig(channel.Guild.Id, guildConfig);
      break;
    }
  }
  
  public static void SetGuildChannel(ulong guildId, ulong channelId) {
    GuildConfig guildConfig = GetGuildConfig(guildId);
    guildConfig.ChannelId = channelId;
    SetGuildConfig(guildId, guildConfig);
    SetUpGuild(guildConfig);
  }

  public static void SetGuildRole(ulong guildId, ulong roleId) {
    GuildConfig guildConfig = GetGuildConfig(guildId);
    guildConfig.RoleId = roleId;
    SetGuildConfig(guildId, guildConfig);
  }

  public static GuildConfig GetGuildConfig(ulong id) {
    KeyValuePair<ulong, GuildConfig>? x = Guilds.FirstOrDefault(g => g.Key == id);

    if (x.HasValue) {
      return x.Value.Value;
    }
    
    KeyValuePair<ulong, GuildConfig> y = new KeyValuePair<ulong, GuildConfig>(id, new GuildConfig());
    Guilds.Add(y);
    return y.Value;
  }

  public static void SetGuildConfig(ulong id, GuildConfig guildConfig) {
    KeyValuePair<ulong, GuildConfig>? x = Guilds.FirstOrDefault(g => g.Key == id);

    if (x.HasValue) {
      Guilds.Remove(x.Value);
    }
    
    KeyValuePair<ulong, GuildConfig> y = new KeyValuePair<ulong, GuildConfig>(id, guildConfig);
    Guilds.Add(y);
  }

  public static ulong SetCount(ulong guildId, GuildConfig guildConfig, ulong count) {
    guildConfig.Count = count;
    SetGuildConfig(guildId, guildConfig);
    return count;
  }

  public static void SetLeniency(ulong guildId, uint leniency) {
    GuildConfig guildConfig = GetGuildConfig(guildId);
    guildConfig.Leniency = leniency;
    SetGuildConfig(guildId, guildConfig);
  }

  static void SetLastAuthorId(ulong guildId, ulong lastAuthorId) {
    GuildConfig guildConfig = GetGuildConfig(guildId);
    guildConfig.LastAuthorId = lastAuthorId;
    SetGuildConfig(guildId, guildConfig);
  }
  
  public static string PrintGuildConfig(GuildConfig guildConfig) {
    return "Guild Config: {\n" +
           $"\tChannelId: {guildConfig.ChannelId}\n" +
           $"\tRoleId: {guildConfig.RoleId}\n" +
           $"\tCount: {guildConfig.Count}\n" +
           $"\tLastAuthorId: {guildConfig.LastAuthorId}\n" +
           $"\tLeniency: {guildConfig.Leniency}"
           + "}";
  }

  public static ulong GetCount(ulong guildId) {
    GuildConfig guildConfig = GetGuildConfig(guildId);

    guildConfig.Count ??= SetCount(guildId, guildConfig, 0);
    
    return guildConfig.Count.Value;
  }
  
  public static uint GetLeniency(ulong guildId) {
    GuildConfig guildConfig = GetGuildConfig(guildId);
    return guildConfig.Leniency;
  }
}

public struct GuildConfig {
  public GuildConfig() {
  }

  public ulong? ChannelId { get; set; } = null;
  public ulong? RoleId { get; set; } = null;
  public ulong? Count { get; set; } = null;
  public ulong? LastAuthorId { get; set; } = null;
  public uint Leniency { get; set; } = 0;
}
