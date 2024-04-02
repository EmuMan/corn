using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.Fonts;
using System.Diagnostics;
using CornBot.Handlers;
using CornBot.Models;
using CornBot.Utilities;
using System.ComponentModel;

namespace CornBot
{
    public class CornClient
    {
        public static IConfiguration? Configuration { get; private set; }

        private readonly IServiceProvider _services;
        private readonly string _botKey;

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
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("cornfig.json", false, false)
                .Build();
            var tempKey = Configuration["discord_token"];
            if (tempKey == null)
            {
                throw new Exception("No bot key found in cornfig.json");
            }
            else
            {
                _botKey = tempKey;
            }

            _services = new ServiceCollection()
                .AddSingleton(this)
                .AddSingleton(_socketConfig)
                .AddSingleton(new Random((int)DateTime.UtcNow.Ticks))
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<InteractionService>()
                .AddSingleton<MessageHandler>()
                .AddSingleton<InteractionHandler>()
                .AddSingleton<ImageManipulator>()
                .AddSingleton<ImageStore>()
                .AddSingleton<HttpClient>()
                .AddSingleton<AppJsonSerializerContext>()
                .AddSingleton<CornAPI>()
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

            await Log(new LogMessage(LogSeverity.Info, "MainAsync", "Configuring HTTP client..."));
            var httpClient = _services.GetRequiredService<HttpClient>();
            httpClient.BaseAddress = new Uri("https://cornbot.azurewebsites.net");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            await Log(LogSeverity.Info, "MainAsync", "Logging in...");
            await client.LoginAsync(TokenType.Bot, _botKey);
            await Log(LogSeverity.Info, "MainAsync", "Starting bot...");
            await client.StartAsync();

            await Task.Delay(Timeout.Infinite);
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

        public async Task<string> GetUserDisplayStringAsync(ulong guildId, ulong userId, bool includeUsername)
        {
            var client = _services.GetRequiredService<DiscordSocketClient>();
            var guild = client.GetGuild(guildId);
            var user = guild.GetUser(userId);
            if (user is null)
            {
                await guild.DownloadUsersAsync();
                user = guild.GetUser(userId);
                if (user is null)
                {
                    return userId.ToString();
                }
            }

            string displayName = user is SocketGuildUser guildUser ?
                guildUser.DisplayName :
                (user.GlobalName ?? user.Username);

            if (includeUsername)
            {
                return displayName == user.Username ? user.Username : $"{displayName} ({user.Username})";
            }
            else
            {
                return displayName;
            }
        }

        private async Task AsyncOnReady()
        {
            await Log(new LogMessage(LogSeverity.Info, "OnReady", "corn has been created"));
            // TODO: daily resets
        }

    }
}
