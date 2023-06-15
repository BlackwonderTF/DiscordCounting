using Discord;
using Discord.WebSocket;
using System.Text.Json;
using SlashHandler = System.Func<Discord.WebSocket.SocketSlashCommand, bool>;

DiscordSocketConfig config = new DiscordSocketConfig {
    MessageCacheSize = 1000,
    GatewayIntents = GatewayIntents.All,
    AlwaysDownloadUsers = true,
};

List<KeyValuePair<ulong, GuildConfig>> guilds = new List<KeyValuePair<ulong, GuildConfig>>();
DiscordSocketClient client = new DiscordSocketClient(config);

#region SlashCommands
List<KeyValuePair<string, SlashHandler>> commands = new List<KeyValuePair<string, SlashHandler>>();

AddSlashHandler("ping", (arg) => {
    arg.RespondAsync("pong!");
    return true;
});

AddSlashHandler("channel", (arg) => {
    SocketGuildChannel channel = (SocketGuildChannel)arg.Data.Options.First().Value;
    SetGuildChannel(channel.Guild.Id, channel.Id);
    arg.RespondAsync(text: $"Counting channel set to {channel.Name}!", ephemeral: true);
    Console.WriteLine($"New guild config {PrintGuildConfig(GetGuildConfig(channel.Guild.Id))}");
    return true;
});

AddSlashHandler("role", (arg) => {
    IRole role = (IRole)arg.Data.Options.First().Value;
    SetGuildRole(role.Guild.Id, role.Id);
    arg.RespondAsync(text: $"Counting role set to {role.Name}!", ephemeral: true);
    Console.WriteLine($"New guild config {PrintGuildConfig(GetGuildConfig(role.Guild.Id))}");
    return true;
});

SlashHandler? GetSlashHandler(string name) {
    KeyValuePair<string, SlashHandler>? x = commands.FirstOrDefault(c => c.Key == name);
    return x?.Value;
}

void AddSlashHandler(string name, SlashHandler handler) {
    KeyValuePair<string, SlashHandler> x = new KeyValuePair<string, SlashHandler>(name, handler);
    commands.Add(x);
}

#endregion

#region functions

void Cleanup() {
    Console.WriteLine("Cleaning up...");
    
    // Exit the client
    client.StopAsync();
    client.LogoutAsync();
    client.Dispose();
    
    // Save the guild configs
    string json = JsonSerializer.Serialize(guilds);
    File.WriteAllText("guilds.json", json);
    
    Console.WriteLine($"Wrote {json} to guilds.json!");
    
    
    Console.WriteLine("Done cleaning up!");
}

async void Login() {
    string token = File.ReadAllText("token.txt");
    await client.LoginAsync(TokenType.Bot, token);
    client.StartAsync();
    
    while (true) {
        Console.CancelKeyPress += delegate {
            Cleanup();
        };
        
        // Close the thread safely
        if (Console.ReadLine() == "exit") {
            Cleanup();
        }
    }
}

void SetGuildChannel(ulong guildId, ulong channelId) {
    GuildConfig guildConfig = GetGuildConfig(guildId);
    guildConfig.ChannelId = channelId;
    SetGuildConfig(guildId, guildConfig);
    SetUpGuild(guildConfig);
}  

void SetGuildRole(ulong guildId, ulong roleId) {
    GuildConfig guildConfig = GetGuildConfig(guildId);
    guildConfig.RoleId = roleId;
    SetGuildConfig(guildId, guildConfig);
}

GuildConfig GetGuildConfig(ulong id) {
    KeyValuePair<ulong, GuildConfig>? x = guilds.FirstOrDefault(g => g.Key == id);

    if (x.HasValue) {
        return x.Value.Value;
    }
    
    KeyValuePair<ulong, GuildConfig> y = new KeyValuePair<ulong, GuildConfig>(id, new GuildConfig());
    guilds.Add(y);
    return y.Value;
}

void SetGuildConfig(ulong id, GuildConfig guildConfig) {
    KeyValuePair<ulong, GuildConfig>? x = guilds.FirstOrDefault(g => g.Key == id);

    if (x.HasValue) {
        guilds.Remove(x.Value);
    }
    
    KeyValuePair<ulong, GuildConfig> y = new KeyValuePair<ulong, GuildConfig>(id, guildConfig);
    guilds.Add(y);
}

void SetCount(ulong guildId, GuildConfig guildConfig, ulong count) {
    guildConfig.Count = count;
    SetGuildConfig(guildId, guildConfig);
}

void SetLastAuthorId(ulong guildId, ulong lastAuthorId) {
    GuildConfig guildConfig = GetGuildConfig(guildId);
    guildConfig.LastAuthorId = lastAuthorId;
    SetGuildConfig(guildId, guildConfig);
}

async Task SetUpGuild(GuildConfig guild) {
    Console.WriteLine(PrintGuildConfig(guild));
        
    if (guild.ChannelId is null) return;

    SocketTextChannel? channel = (SocketTextChannel)client.GetChannel(guild.ChannelId.Value);
        
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

Task SlashCommandHandler(SocketSlashCommand arg) {
    GetSlashHandler(arg.Data.Name)?.Invoke(arg);
    return Task.CompletedTask;
}

async Task ClientReady() {
    Console.WriteLine("Counting bot logged in!");
    await client.SetActivityAsync(new Game("Checking your counting skills!"));
    
    // Read the guilds from the file
    string json = File.ReadAllText("guilds.json");
    guilds = JsonSerializer.Deserialize<List<KeyValuePair<ulong, GuildConfig>>>(json) ?? new List<KeyValuePair<ulong, GuildConfig>>();

    // Read the last count from the counting channel
    foreach (KeyValuePair<ulong, GuildConfig> guild in guilds) {
        Console.WriteLine($"Checking guild {guild.Key}...");
        await SetUpGuild(guild.Value);
    }
}

Task MessageReceived(SocketMessage msg) {
    if (msg.Author.IsBot) {
        return Task.CompletedTask;
    }

    if (!(msg.Channel is SocketGuildChannel channel && channel.GetChannelType() is ChannelType.Text)) {
        return Task.CompletedTask;
    }

    SocketGuild guild = channel.Guild;
    GuildConfig guildConfig = GetGuildConfig(guild.Id);
    
    // Check if bot is set up
    if (!guildConfig.RoleId.HasValue || !guildConfig.ChannelId.HasValue) {
        return Task.CompletedTask;
    }
    
    // Check if message is in the right channel
    if (guildConfig.ChannelId.HasValue && guildConfig.ChannelId.Value != channel.Id) {
        return Task.CompletedTask;
    }
    
    // Check if the previous message was by the same author
    if (guildConfig.LastAuthorId.HasValue && guildConfig.LastAuthorId.Value == msg.Author.Id) {
        // Add role to user
        (msg.Author as SocketGuildUser)?.AddRoleAsync(guild.GetRole(guildConfig.RoleId.Value));

        // Send a message
        msg.Channel.SendMessageAsync($"You are not allowed to post twice!\nThe count has been reset to 0.", messageReference: new MessageReference(msg.Id, channel.Id, guild.Id));

        // Reset the count
        SetCount(guild.Id, guildConfig, 0);
        return Task.CompletedTask;
    }
    
    guildConfig.LastAuthorId = msg.Author.Id;
    SetGuildConfig(guild.Id, guildConfig);

    // Check if message is the correct number
    string firstWord = msg.CleanContent.Split(' ')[0];

    if (guildConfig.Count is null) {
        if (!ulong.TryParse(firstWord, out ulong count)) {
            return Task.CompletedTask;
        }
        
        guildConfig.Count = count;
        SetGuildConfig(guild.Id, guildConfig);
        return Task.CompletedTask;
    }
    
    bool startsWith = msg.CleanContent.StartsWith((guildConfig.Count + 1).ToString()!);

    // If not, reset the count
    if (!startsWith) {
        // Add role to user
        (msg.Author as SocketGuildUser)?.AddRoleAsync(guild.GetRole(guildConfig.RoleId.Value));

        // Send a message
        msg.Channel.SendMessageAsync($"You messed up! The next number was {guildConfig.Count + 1}! ({firstWord}).\nThe count has been reset to 0.", messageReference: new MessageReference(msg.Id, channel.Id, guild.Id));

        // Reset the count
        SetCount(guild.Id, guildConfig, 0);
        return Task.CompletedTask;
    }
    
    // If so, increment the count
    ulong newCount = guildConfig.Count.Value + 1;
    guildConfig.Count = newCount;
    SetGuildConfig(guild.Id, guildConfig);
    
    // If the count is a a power of 10, remove the role from all users
    if (newCount > 10 && Math.Abs(Math.Pow(10, Math.Log10(newCount)) - newCount) < 0.0000001) {
        Console.WriteLine("Power of 10 reached!");
        // foreach (SocketGuildUser user in guild.Users) {
        //     await user.RemoveRoleAsync(guild.GetRole(guildConfig.RoleId.Value));
        // }
    }

    return Task.CompletedTask;
}

#endregion

#region login
client.MessageReceived += MessageReceived;
client.SlashCommandExecuted += SlashCommandHandler;
client.Ready += ClientReady;

Login();

#endregion

#region Helper

string PrintGuildConfig(GuildConfig guildConfig) {
    return "Guild Config: {\n" +
           $"\tChannelId: {guildConfig.ChannelId}\n" +
           $"\tRoleId: {guildConfig.RoleId}\n" +
           $"\tCount: {guildConfig.Count}\n" +
           $"\tLastAuthorId: {guildConfig.LastAuthorId}\n"
           + "}";
}

#endregion

#region structs
struct GuildConfig {
    public GuildConfig() {
    }

    public ulong? ChannelId { get; set; } = null;

    public ulong? RoleId { get; set; } = null;

    public ulong? Count { get; set; } = 0;
    
    public ulong? LastAuthorId { get; set; } = null;
}

#endregion


