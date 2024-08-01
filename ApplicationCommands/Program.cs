// See https://aka.ms/new-console-template for more information

using Discord;
using Discord.Net;
using Discord.WebSocket;
using Helper;
using Newtonsoft.Json;

var running = true;

var config = new DiscordSocketConfig {
  MessageCacheSize = 1000,
  GatewayIntents = GatewayIntents.All,
  AlwaysDownloadUsers = true,
};

var client = new DiscordSocketClient(config);
Config.SetClient(client);

client.Ready += async () => {
  Console.WriteLine("Creating slash commands...");
  var slashCommands = CommandStorage.Commands.Values
    .Where(command => command.IsSlashCommand)
    .Select(command => command.BuildSlashCommand())
    .ToList();
  
  var userCommands = CommandStorage.Commands.Values
    .Where(command => command.IsUserCommand)
    .Select(command => command.BuildUserCommand())
    .ToList();
  
  
  try {
    var globalSlashCommands = slashCommands.Select(x => x.Build()).Cast<ApplicationCommandProperties>();
    var globalUserCommands = userCommands.Select(x => x.Build()).Cast<ApplicationCommandProperties>();

    var allCommands = globalSlashCommands
      .Concat(globalUserCommands)
      .ToArray();
  
    try {
      Console.WriteLine("Registering commands...");
      await client.BulkOverwriteGlobalApplicationCommandsAsync(allCommands);
    } catch (HttpException e) {
      var json = JsonConvert.SerializeObject(e.Errors, Formatting.Indented);
      Console.WriteLine(json);
    }
  } catch (Exception e) {
    Console.WriteLine(e);
    throw;
  }
  

  Console.WriteLine("Done registering commands!");
  Environment.Exit(0);
};

Login();
return;

async void Login() {
  var token = File.ReadAllText("token.txt");
  await client.LoginAsync(TokenType.Bot, token);
  await client.StartAsync();
  Console.WriteLine("Finished logging in!");
  
  Thread thread = new(() => {
    while (running) {
      // Close the thread safely
      if (Console.ReadLine() == "exit") {
        running = false;
      }
    }
  });
  thread.Start();
}
