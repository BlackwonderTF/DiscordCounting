// See https://aka.ms/new-console-template for more information

using Discord;
using Discord.Net;
using Discord.WebSocket;
using Helper;
using Newtonsoft.Json;

bool running = true;

DiscordSocketConfig config = new DiscordSocketConfig {
  MessageCacheSize = 1000,
  GatewayIntents = GatewayIntents.All,
  AlwaysDownloadUsers = true,
};

DiscordSocketClient client = new DiscordSocketClient(config);
Config.SetClient(client);

client.Ready += async () => {
  Console.WriteLine("Creating slash commands...");
  List<SlashCommandBuilder> commands = CommandStorage.Commands.Values.Select(command => command.Build()).ToList();
  try {
    ApplicationCommandProperties[] globalCommands = commands.Select(x => x.Build()).Cast<ApplicationCommandProperties>().ToArray();
  
    try {
      Console.WriteLine("Registering slash commands...");
      await client.BulkOverwriteGlobalApplicationCommandsAsync(globalCommands);
    } catch (HttpException e) {
      string json = JsonConvert.SerializeObject(e.Errors, Formatting.Indented);
      Console.WriteLine(json);
    }
  } catch (Exception e) {
    Console.WriteLine(e);
    throw;
  }
  

  Console.WriteLine("Done registering slash commands!");
  Environment.Exit(0);
};

Login();

async void Login() {
  string token = File.ReadAllText("token.txt");
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
