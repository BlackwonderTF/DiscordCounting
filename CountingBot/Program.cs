using Discord;
using Discord.WebSocket;
using Helper;
using SlashHandler = System.Func<Discord.WebSocket.SocketSlashCommand, bool>;

DiscordSocketConfig config = new DiscordSocketConfig {
    MessageCacheSize = 1000,
    GatewayIntents = GatewayIntents.All,
    AlwaysDownloadUsers = true,
};

DiscordSocketClient client = new DiscordSocketClient(config);
Config.SetClient(client);

#region functions
void Cleanup() {
    Console.WriteLine("Cleaning up...");
    
    // Exit the client
    client.StopAsync();
    client.LogoutAsync();
    client.Dispose();
    
    // Save the guild configs
    string json = Config.Serialize();
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

Task SlashCommandHandler(SocketSlashCommand arg) {
    CommandStorage.ExecuteCommand(arg);
    return Task.CompletedTask;
}

async Task ClientReady() {
    Console.WriteLine("Counting bot logged in!");
    await client.SetActivityAsync(new Game("Checking your counting skills!"));
    
    // Read the guilds from the file
    string json = File.ReadAllText("guilds.json");
    Config.DeSerialize(json);

    // Read the last count from the counting channel
    foreach (KeyValuePair<ulong, GuildConfig> guild in Config.Guilds) {
        Console.WriteLine($"Checking guild {guild.Key}...");
        await Config.SetUpGuild(guild.Value);
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
    GuildConfig guildConfig = Config.GetGuildConfig(guild.Id);
    
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
        const string message = "You are not allowed to post twice!\nThe count has been reset to 0.";
        Reset(msg, guild, guildConfig, channel, message);
        return Task.CompletedTask;
    }
    
    guildConfig.LastAuthorId = msg.Author.Id;
    Config.SetGuildConfig(guild.Id, guildConfig);

    // Check if message is the correct number
    string firstWord = msg.CleanContent.Split(' ')[0];

    if (guildConfig.Count is null) {
        if (!ulong.TryParse(firstWord, out ulong count)) {
            return Task.CompletedTask;
        }
        
        guildConfig.Count = count;
        Config.SetGuildConfig(guild.Id, guildConfig);
        return Task.CompletedTask;
    }
    
    ulong newCount = guildConfig.Count.Value + 1;
    
    Console.WriteLine(newCount);
    
    // If not, reset the count
    if (firstWord != (guildConfig.Count + 1).ToString()!) {
        string message = $"You messed up! The next number was {guildConfig.Count + 1}! ({firstWord}).\nThe count has been reset to 0.";
        Reset(msg, guild, guildConfig, channel, message);
        return Task.CompletedTask;
    }
    
    // If so, increment the count
    
    guildConfig.Count = newCount;
    Config.SetGuildConfig(guild.Id, guildConfig);
    
    // // If the count is a a power of 10, remove the role from all users
    // if (newCount > 10 && Math.Abs(Math.Pow(10, Math.Log10(newCount)) - newCount) < 0.0000001) {
    //     Console.WriteLine("Power of 10 reached!");
    // }

    return Task.CompletedTask;
}

void Reset(SocketMessage msg, SocketGuild guild, GuildConfig guildConfig, SocketGuildChannel channel, string message) {
    // Check for leniency
    if (Config.GetCount(guild.Id) < Config.GetLeniency(guild.Id)) {
        msg.Channel.SendMessageAsync("Leniency active...", messageReference: new MessageReference(msg.Id, channel.Id, guild.Id));
    } else {
        // Add role to user
        if (guildConfig.RoleId is not null) {
            (msg.Author as SocketGuildUser)?.AddRoleAsync(guild.GetRole(guildConfig.RoleId.Value));
        }
    }

    // Send a message
    msg.Channel.SendMessageAsync(message, messageReference: new MessageReference(msg.Id, channel.Id, guild.Id));

    // Reset the count
    Config.SetCount(guild.Id, guildConfig, 0);
}

#endregion

#region login
client.MessageReceived += MessageReceived;
client.SlashCommandExecuted += SlashCommandHandler;
client.Ready += ClientReady;

Login();

#endregion


