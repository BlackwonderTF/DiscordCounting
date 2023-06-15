// See https://aka.ms/new-console-template for more information

using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

bool running = true;

DiscordSocketConfig config = new DiscordSocketConfig {
  MessageCacheSize = 1000,
  GatewayIntents = GatewayIntents.All,
  AlwaysDownloadUsers = true,
};

DiscordSocketClient client = new DiscordSocketClient(config);

client.Ready += async () => {
  Console.WriteLine("Registering slash commands...");

  List<SlashCommandBuilder> commands = new List<SlashCommandBuilder>() {
    new SlashCommandBuilder()
      .WithName("ping")
      .WithDescription("Pong!"),
    new SlashCommandBuilder()
      .WithName("channel")
      .WithDescription("Set the counting channel")
      .AddOption(new SlashCommandOptionBuilder()
        .WithName("channel")
        .WithDescription("The channel to set")
        .WithRequired(true)
        .WithType(ApplicationCommandOptionType.Channel)),
    new SlashCommandBuilder()
      .WithName("role")
      .WithDescription("Set the counting role")
      .AddOption(new SlashCommandOptionBuilder()
        .WithName("role")
        .WithDescription("The role to set")
        .WithRequired(true)
        .WithType(ApplicationCommandOptionType.Role))
  };
    
  foreach (SlashCommandBuilder command in commands) {
    try {
      Console.WriteLine($"Registering slash command: {command.Name}...");
      await client.CreateGlobalApplicationCommandAsync(command.Build());
    } catch (HttpException e) {
      string json = JsonConvert.SerializeObject(e.Errors, Formatting.Indented);
      Console.WriteLine(json);
    }
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
