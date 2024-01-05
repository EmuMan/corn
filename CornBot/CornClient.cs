using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.Fonts;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using System.Diagnostics;
using CornBot.Handlers;
using CornBot.Models;
using CornBot.Serialization;
using CornBot.Services;
using CornBot.Utilities;
using SQLitePCL;

// cornfig.Local.json format
// {
//     "BotKey" : "######..."
// }

namespace CornBot
{
    public class CornClient
    {
        public static string BOT_KEY = "";

        public static IConfiguration? Configuration;

        private readonly IServiceProvider _services;

        private readonly DiscordSocketConfig _socketConfig = new()
        {
            GatewayIntents = GatewayIntents.Guilds |
                             GatewayIntents.GuildMembers |
                             GatewayIntents.GuildEmojis |
                             GatewayIntents.GuildMessages |
                             GatewayIntents.DirectMessages |
                             GatewayIntents.MessageContent,
            AlwaysDownloadUsers = true,
        };

        public CornClient()
        {
#if DEBUG
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("cornfig.Development.json", false, false)
                .Build();


#else
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("cornfig.Production.json", false, false)
                .Build();
#endif
            var keyVaultUri = Configuration["KeyVaultUri"] ?? throw new ArgumentNullException("KeyVaultUri");
            var keyName = Configuration["KeyName"] ?? throw new ArgumentNullException("KeyName");
            var client = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
            BOT_KEY = client.GetSecret(keyName).Value.Value;

            _services = new ServiceCollection()
                .AddSingleton(this)
                .AddSingleton(_socketConfig)
                .AddSingleton(new Random((int)DateTime.UtcNow.Ticks))
                .AddSingleton<GuildTrackerSerializer>()
                .AddSingleton<GuildTracker>()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<InteractionService>()
                .AddSingleton<MessageHandler>()
                .AddSingleton<InteractionHandler>()
                .AddSingleton<ImageManipulator>()
                .AddSingleton<ImageStore>()
                .AddSingleton<CornAPI>()
                .AddSingleton<MqttService>()
                .BuildServiceProvider();
        }

        public async Task MainAsync()
        {
            var client = _services.GetRequiredService<DiscordSocketClient>();

            client.Log += Log;
            client.Ready += AsyncOnReady;

            await Log(LogSeverity.Info, "MainAsync", "Initializing handlers...");
            await _services.GetRequiredService<MessageHandler>().Initialize();
            await _services.GetRequiredService<InteractionHandler>().InitializeAsync();

            await Log(LogSeverity.Info, "MainAsync", "Initializing serializer...");
            _services.GetRequiredService<GuildTrackerSerializer>().Initialize("userdata.db");
            await Log(LogSeverity.Info, "MainAsync", "Loading guilds...");
            await _services.GetRequiredService<GuildTracker>().LoadFromSerializer();

            await Log(LogSeverity.Info, "MainAsync", "Loading fonts...");
            var imageManipulator = _services.GetRequiredService<ImageManipulator>();
            imageManipulator.LoadFont("Assets/Consolas.ttf", 72, FontStyle.Regular);
            imageManipulator.AddFallbackFontFamily("Assets/NotoEmoji-Bold.ttf");
            string[] notoSansFiles = Directory.GetFiles("Assets/notosans", "*.ttf", SearchOption.TopDirectoryOnly);
            await Log(new LogMessage(LogSeverity.Info, "MainAsync", $"Loading {notoSansFiles.Length} Noto Sans files..."));
            foreach (var file in notoSansFiles)
                imageManipulator.AddFallbackFontFamily(file);
            await Log(new LogMessage(LogSeverity.Info, "MainAsync", "Testing fallback fonts..."));
            imageManipulator.TestAllFallback();
            await Log(new LogMessage(LogSeverity.Info, "MainAsync", "Finished testing fallback fonts."));

            await Log(new LogMessage(LogSeverity.Info, "MainAsync", "Loading images..."));
            await _services.GetRequiredService<ImageStore>().LoadImages();

            await Log(LogSeverity.Info, "MainAsync", "Logging in...");
            await client.LoginAsync(TokenType.Bot, BOT_KEY);
            await Log(LogSeverity.Info, "MainAsync", "Starting bot...");
            await client.StartAsync();


            await Log(LogSeverity.Info, "MainAsync", "Starting MQTT service...");
            await _services.GetRequiredService<MqttService>().RunAsync();

            await Log(LogSeverity.Info, "MainAsync", "Starting API service...");
            var api = _services.GetRequiredService<CornAPI>();
            await api.RunAsync(); // Does not return
        }

        public Task Log(LogMessage msg)
        {
            if (msg.Exception is CommandException cmdException)
            {
                Console.WriteLine($"[Command/{msg.Severity}] {cmdException.Command.Aliases.First()}"
                                 + $" failed to execute in {cmdException.Context.Channel}.");
                Console.WriteLine(cmdException);
            }
            else
                Console.WriteLine($"[General/{msg.Severity}] {msg}");
            return Task.CompletedTask;
        }

        public Task Log(LogSeverity severity, string source, string message, Exception? exception = null)
        {
            return Log(new LogMessage(severity, source, message, exception));
        }

        private async Task AsyncOnReady()
        {
            await Log(new LogMessage(LogSeverity.Info, "OnReady", "corn has been created"));
            // TODO: verify that this definitely works and properly broadcasts exceptions
            _ = _services.GetRequiredService<GuildTracker>().StartDailyResetLoop()
                .ContinueWith(t => Log(new LogMessage(LogSeverity.Critical, "ResetLoop", "Daily reset loop failed, aborting.", t.Exception)),
                              TaskContinuationOptions.OnlyOnFaulted);
        }

    }
}
