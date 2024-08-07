﻿using Discord;
using Discord.WebSocket;
using Helper;
using SlashHandler = System.Func<Discord.WebSocket.SocketSlashCommand, bool>;

var config = new DiscordSocketConfig {
  MessageCacheSize = 1000,
  GatewayIntents = GatewayIntents.All,
  AlwaysDownloadUsers = true,
};

var client = new DiscordSocketClient(config);
Config.SetClient(client);

#region functions
void Cleanup() {
  Console.WriteLine("Cleaning up...");
    
  // Exit the client
  client.StopAsync();
  client.LogoutAsync();
  client.Dispose();
    
  // Save the guild configs
  var json = Config.Serialize();
  File.WriteAllText("guilds.json", json);
    
  Console.WriteLine($"Wrote {json} to guilds.json!");
  Console.WriteLine("Done cleaning up!");
}

async void Login() {
  var token = File.ReadAllText("token.txt");
  await client.LoginAsync(TokenType.Bot, token);
  _ = client.StartAsync();
    
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

Task UserCommandHandler(SocketUserCommand arg) {
  CommandStorage.ExecuteCommand(arg, client);
  return Task.CompletedTask;
}

Task SlashCommandHandler(SocketSlashCommand arg) {
  CommandStorage.ExecuteCommand(arg, client);
  return Task.CompletedTask;
}

async Task ClientReady() {
  Console.WriteLine("Counting bot logged in!");
  await client.SetActivityAsync(new Game("Checking your counting skills!"));
    
  // Read the guilds from the file
  var json = File.ReadAllText("guilds.json");
  Config.DeSerialize(json);

  // Read the last count from the counting channel
  foreach (var guild in Config.Guilds) {
    Console.WriteLine($"Checking guild {guild.Key}...");
    await Config.SetUpGuild(guild.Value);
  }
}

Task GuildMemberUpdated(Cacheable<SocketGuildUser, ulong> oldCached, SocketGuildUser newUser) {
  if (!oldCached.HasValue) {
    return Task.CompletedTask;;
  }
    
  var oldUser = oldCached.Value;
  if (Equals(oldUser.Roles.Select(x => x.Id), newUser.Roles.Select(x => x.Id))) {
    return Task.CompletedTask;
  }
    
  var guildConfig = Config.GetGuildConfig(newUser.Guild.Id);
  var newUserRoles = newUser.Roles.Select(x => x.Id).ToList();
  var oldUserRoles = oldUser.Roles.Select(x => x.Id).ToList();
    
  // Check which roles got added/removed
  var addedRoles = newUserRoles.Except(oldUserRoles).ToList();
  var removedRoles = oldUserRoles.Except(newUserRoles).ToList();
    
  // Check if the user got the counting role
  if (guildConfig.RoleId is not null && addedRoles.Contains(guildConfig.RoleId.Value)) {
    // Add the user to the innumerate list
    Config.AddInnumerate(newUser.Guild.Id, newUser.Id);
  }
    
  // Check if the user got the counting role removed
  if (guildConfig.RoleId is not null && removedRoles.Contains(guildConfig.RoleId.Value)) {
    // Remove the user from the innumerate list
    Config.RemoveInnumerate(newUser.Guild.Id, newUser.Id);
  }
    
  // Check if the user got a secondary role
  foreach (var roleId in addedRoles.Where(roleId => guildConfig.SecondaryRoles.Contains(roleId))) {
    Config.AddSecondaryRole(newUser.Guild.Id, newUser.Id, roleId);
  }
    
  // Check if the user got a secondary role removed
  foreach (var roleId in removedRoles.Where(roleId => guildConfig.SecondaryRoles.Contains(roleId))) {
    Config.RemoveSecondaryRole(newUser.Guild.Id, newUser.Id, roleId);
  }

  return Task.CompletedTask;
}

Task UserJoined(SocketGuildUser socketGuildUser) {
  var guildConfig = Config.GetGuildConfig(socketGuildUser.Guild.Id);
  SecondaryRoleCheck(socketGuildUser, guildConfig);
    
  var isInnumerate = Config.IsInnumerate(socketGuildUser.Guild.Id, socketGuildUser.Id);

  if (!isInnumerate) {
    return Task.CompletedTask;
  }

  if (guildConfig.RoleId is null) {
    return Task.CompletedTask;
  }
    
  socketGuildUser.AddRoleAsync(guildConfig.RoleId.Value);

  return Task.CompletedTask;
}

void SecondaryRoleCheck(IGuildUser socketGuildUser, GuildConfig guildConfig) {
  if (guildConfig.SecondaryRoles.Count == 0) {
    return;
  }

  if (!guildConfig.SecondaryRolesStorage.TryGetValue(socketGuildUser.Id, out var roles)) {
    return;
  }

  foreach (var roleId in roles) {
    socketGuildUser.AddRoleAsync(roleId);
  }
}

bool BotIsActive(SocketMessage msg, out SocketGuild? guild, out GuildConfig? guildConfig, out SocketTextChannel? outChannel) {
  guild = null;
  guildConfig = null;
  outChannel = null;
    
  if (msg.Author.IsBot || msg.Author.IsWebhook || msg.Content.Length == 0) {
    return false;
  }

  if (!(msg.Channel is SocketTextChannel channel && channel.GetChannelType() is ChannelType.Text)) {
    return false;
  }

  guild = channel.Guild;
  guildConfig = Config.GetGuildConfig(guild.Id);
  outChannel = channel;
    
  // Check if bot is set up and enabled
  if (!guildConfig.Value.IsEnabled || !guildConfig.Value.IsSetUp) {
    return false;
  }
    
  // Check if message is in the right channel
  if (guildConfig.Value.ChannelId.HasValue && guildConfig.Value.ChannelId.Value != channel.Id) {
    return false;
  }

  return true;
}

Task MessageReceived(SocketMessage msg) {
  if (!BotIsActive(msg, out var checkGuild, out var checkGuildConfig, out var checkChannel)) {
    return Task.CompletedTask;
  }

  if (checkGuild is null || checkGuildConfig is null || checkChannel is null) {
    return Task.CompletedTask;
  }

  var guildConfig = (GuildConfig)checkGuildConfig;

  // Check if the previous message was by the same author
  if (guildConfig.LastAuthorId.HasValue && guildConfig.LastAuthorId.Value == msg.Author.Id) {
    const string message = "You are not allowed to post twice!\nThe count has been reset to 0.";
    Reset(msg, checkGuild, guildConfig, checkChannel, message);
    return Task.CompletedTask;
  }
    
  guildConfig.LastAuthorId = msg.Author.Id;
  Config.SetGuildConfig(checkGuild.Id, guildConfig);

  // Check if message is the correct number
  var firstWord = msg.CleanContent.Split(' ')[0];

  if (guildConfig.Count is null) {
    if (!ulong.TryParse(firstWord, out var count)) {
      return Task.CompletedTask;
    }
        
    guildConfig.Count = count;
    Config.SetGuildConfig(checkGuild.Id, guildConfig);
    return Task.CompletedTask;
  }
    
  var newCount = guildConfig.Count.Value + 1;
    
  // If not, reset the count
  if (firstWord != newCount.ToString()) {
    var message = $"You messed up! The next number was {guildConfig.Count + 1}! ({firstWord}).\nThe count has been reset to 0.";
    Reset(msg, checkGuild, guildConfig, checkChannel, message);
    return Task.CompletedTask;
  }
  
  // Check if the next number is in the chain
  if (msg.CleanContent.Contains($"{newCount + 1}"))
  {
    const string message = "You messed up! \"You can type afterwards within the same post, but do not include the next number in the chain within the text\"!\nThe count has been reset to 0.";
    Reset(msg, checkGuild, guildConfig, checkChannel, message);
    return Task.CompletedTask;
  }
    
  // If so, increment the count
  guildConfig.Count = newCount;
  Config.SetGuildConfig(checkGuild.Id, guildConfig);

  return Task.CompletedTask;
}

async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel _) {
  if (!BotIsActive(after, out var checkGuild, out var checkGuildConfig, out var checkChannel)) {
    return;
  }
    
  if (checkGuild is null || checkGuildConfig is null || checkChannel is null) {
    return;
  }
    
  var guildConfig = (GuildConfig)checkGuildConfig!;
  var channel = checkChannel!;
    
  if (!guildConfig.EditResets) {
    return;
  }
    
  // Check if the updated message is the last message
  IAsyncEnumerable<IReadOnlyCollection<IMessage>>? lastMessage = channel.GetMessagesAsync(1);
  if (lastMessage is null) {
    return;
  }
    
  IReadOnlyCollection<IMessage>? lastMessageList = await lastMessage.FirstOrDefaultAsync();

  var lastMessageObj = lastMessageList?.FirstOrDefault();
  if (lastMessageObj is null) {
    return;
  }
    
  if (lastMessageObj.Id != after.Id) {
    return;
  }
    
  // Reset the count
  const string message = "You are not allowed to edit your message!\nThe count has been reset.";
  Reset(after, checkGuild, guildConfig, channel, message);
}

void Reset(SocketMessage msg, SocketGuild guild, GuildConfig guildConfig, SocketGuildChannel channel, string message) {
  // Check for leniency
  if (Config.GetCount(guild.Id) < Config.GetLeniency(guild.Id)) {
    msg.Channel.SendMessageAsync("Leniency active...", messageReference: new MessageReference(msg.Id, channel.Id, guild.Id));
  } else {
    // Add role to user
    if (guildConfig.RoleId is not null && msg.Author is SocketGuildUser author) {
      Config.AddInnumerate(guild.Id, author);
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
client.MessageUpdated += MessageUpdated;
client.Ready += ClientReady;
client.UserJoined += UserJoined;
client.GuildMemberUpdated += GuildMemberUpdated;

client.SlashCommandExecuted += SlashCommandHandler;
client.UserCommandExecuted += UserCommandHandler;

Login();

#endregion
