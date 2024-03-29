using CornBot.Models;
using CornBot.Utilities;
using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using CornBot.Models.Responses;
using CornBot.Models.Requests;

namespace CornBot.Modules
{

    public class EconomyModule : InteractionModuleBase<SocketInteractionContext>
    {
        
        public InteractionService? Commands { get; set; }
        private readonly IServiceProvider _services;

        public EconomyModule(IServiceProvider services)
        {
            _services = services;
            _services.GetRequiredService<CornClient>().Log(
                LogSeverity.Debug, "Modules", "Creating EconomyModule...");
        }

        [CommandContextType(InteractionContextType.Guild)]
        [SlashCommand("corn", "Gets your total corn count")]
        public async Task Corn([Summary(description: "user to lookup")] IUser? user = null)
        {
            var name = Events.GetCurrentName();

            var api = _services.GetRequiredService<CornAPI>();
            var userInfo = await api.GetModelAsync<User>($"/users/{Context.Guild.Id}/{Context.User.Id}");

            var cornEmoji = Events.GetCurrentEmoji();
            var stringId = user is null ? "you have" :
                    user is not SocketGuildUser guildUser ? $"{user} has" :
                    $"{guildUser.DisplayName} ({guildUser}) has";
            await RespondAsync($"{cornEmoji} {stringId} {userInfo.CornCount} {name} {cornEmoji}");
        }

        [CommandContextType(InteractionContextType.Guild)]
        [SlashCommand("daily", "Performs your daily shucking of corn")]
        public async Task Daily()
        {
            var api = _services.GetRequiredService<CornAPI>();
            var response = await api.PostModelAsync<DailyResponse>($"/daily/{Context.Guild.Id}/{Context.User.Id}/complete");
            await RespondAsync($"{response.Message}");
        }

        [CommandContextType(InteractionContextType.Guild)]
        [SlashCommand("lb", "Alias for /leaderboard")]
        public async Task LeaderboardAlias() => await Leaderboard();

        [CommandContextType(InteractionContextType.Guild)]
        [SlashCommand("leaderboard", "Displays the top corn havers in the guild")]
        public async Task Leaderboard()
        {
            // TODO: fix

            var api = _services.GetRequiredService<CornAPI>();
            var leaderboard = api.GetModelAsync<List<User>>($"/leaderboard/{Context.Guild.Id}");

            var embed = new EmbedBuilder()
                .WithColor(Color.Gold)
                .WithThumbnailUrl(Constants.CORN_THUMBNAIL_URL)
                .WithTitle($"Top {name} havers:")
                .WithDescription(await guild.GetLeaderboardsString())
                .WithCurrentTimestamp()
                .Build();

            await RespondAsync(embeds: new Embed[] { embed });
        }

        [CommandContextType([InteractionContextType.Guild, InteractionContextType.BotDm])]
        [SlashCommand("total", "Gets the total corn count across all servers")]
        public async Task Total()
        {
            // TODO: implement this command!
            await RespondAsync($"this command has not been implemented yet!");
        }

        [CommandContextType(InteractionContextType.Guild)]
        [SlashCommand("stats", "Gets an overview of your recent corn shucking")]
        public async Task Stats([Summary(description: "user to lookup")] IUser? user = null)
        {
            var api = _services.GetRequiredService<CornAPI>();
            var history = await api.GetModelAsync<HistorySummary>($"/history/{Context.Guild.Id}/{Context.User.Id}");

            EmbedFieldBuilder[] fields =
            [
                new EmbedFieldBuilder()
                    .WithName("Daily Count")
                    .WithValue($"{history.DailyCount[Context.Guild.Id]:n0} " +
                        $"({history.DailyCountGlobal:n0})")
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Daily Average")
                    .WithValue($"{history.DailyAverage[Context.Guild.Id]:n2} " +
                        $"({history.DailyAverageGlobal:n2})")
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Daily Total")
                    .WithValue($"{history.DailyTotal[Context.Guild.Id]:n0} " +
                        $"({history.DailyTotalGlobal:n0})")
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Longest Daily Streak")
                    .WithValue($"{history.LongestDailyStreak[Context.Guild.Id]:n0} " +
                        $"({history.LongestDailyStreakGlobal:n0})")
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Current Daily Streak")
                    .WithValue($"{history.CurrentDailyStreak[Context.Guild.Id]:n0} " +
                        $"({history.CurrentDailyStreakGlobal:n0})")
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Message Total")
                    .WithValue($"{history.MessageTotal[Context.Guild.Id]:n0} " +
                        $"({history.MessageTotalGlobal:n0})")
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Server Total")
                    .WithValue(userInfo.CornCount.ToString("n0"))
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Global Total")
                    .WithValue(economy.GetTotalCorn(user).ToString("n0"))
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Cornucopia Net Gain")
                    .WithValue($"{history.CornucopiaTotal[Context.Guild.Id]:n0} " +
                        $"({history.CornucopiaTotalGlobal:n0})")
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Cornucopia Percent")
                    .WithValue($"{history.CornucopiaPercent[Context.Guild.Id]*100.0:n2}% " +
                        $"({history.CornucopiaPercentGlobal*100.0:n2}%)")
                    .WithIsInline(true),
            ];

            var displayName = Events.GetUserDisplayString(user, false);

            var author = new EmbedAuthorBuilder()
                .WithIconUrl(user.GetAvatarUrl())
                .WithName(user.Username);

            // TODO: fix
            var embed = new EmbedBuilder()
                .WithTitle($"{displayName}'s {currencyName} stats")
                .WithDescription("*server (global)*")
                .WithAuthor(author)
                .WithThumbnailUrl(Constants.CORN_THUMBNAIL_URL)
                .WithCurrentTimestamp()
                .WithColor(Color.Gold)
                .WithFields(fields)
                .Build();

            await RespondAsync(embeds: new Embed[] { embed });
        }

        [CommandContextType(InteractionContextType.Guild)]
        [SlashCommand("cornucopia", "play a game of slots to gamble your corn")]
        public async Task Cornucopia([Summary(description: "amount of corn to gamble")] long amount)
        {
            var api = _services.GetRequiredService<CornAPI>();
            var result = await api.PostModelAsync<CornucopiaRequest, CornucopiaResponse>(
                $"/cornucopia/{Context.Guild.Id}/{Context.User.Id}/complete",
                new CornucopiaRequest(amount));

            // TODO: fix

            if (numberInDay >= 3)
                await RespondAsync("what are you trying to do, feed your gambling addiction?");
            else if (amount < 1)
                await RespondAsync($"you can't gamble less than 1 {name}.");
            else if (amount > userInfo.CornCount)
                await RespondAsync($"you don't have that much {name}.");
            else if (amount > maxAmount)
                await RespondAsync($"you can't gamble more than {maxAmount} {name} at a time.");
            else
            {
                SlotMachine slotMachine = new(3, amount, random);

                var author = new EmbedAuthorBuilder()
                    .WithIconUrl(Context.User.GetAvatarUrl())
                    .WithName(Context.User.ToString());
                var embed = new EmbedBuilder()
                    .WithDescription(slotMachine.RenderToString(0, numberInDay))
                    .WithAuthor(author)
                    .WithThumbnailUrl(Constants.CORN_THUMBNAIL_URL)
                    .WithCurrentTimestamp()
                    .WithColor(Color.Gold);

                await RespondAsync(embeds: new Embed[] { embed.Build() });
                await userInfo.UpdateForGambling(amount, slotMachine.GetWinnings());
                await userInfo.Save();
                while (slotMachine.RevealProgress < slotMachine.Size)
                {
                    await Task.Delay(2000);
                    slotMachine.RevealProgress++;
                    embed.Description = slotMachine.RenderToString(userInfo.CornCount, numberInDay);
                    await ModifyOriginalResponseAsync(m => m.Embed = embed.Build());
                }

            }
        }


        [CommandContextType(InteractionContextType.Guild)]
        [SlashCommand("cornucopia-max", "play a game of slots with the highest allowed amount of corn")]
        public async Task CornucopiaMax()
        {
            // TODO: implement
        }

    }
}
