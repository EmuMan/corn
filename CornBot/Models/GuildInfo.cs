using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CornBot.Utilities;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace CornBot.Models
{
    public class GuildInfo
    {

        public GuildTracker GuildTracker { get; init; }
        public ulong GuildId { get; init; }
        public int Dailies { get; set; }
        public ulong AnnouncementChannel { get; set; }

        private readonly IServiceProvider _services;

        public GuildInfo(GuildTracker tracker, ulong guildId, int dailies,
            ulong announcementChannel, IServiceProvider services)
        {
            GuildTracker = tracker;
            GuildId = guildId;
            Dailies = dailies;
            AnnouncementChannel = announcementChannel;
            _services = services;
        }

        public async Task<UserInfo> LookupUser(ulong userId)
        {
            var result = await GuildTracker.Serializer.GetUser(this, userId);
            if (result == null)
            {
                result = new(this, userId, _services);
                if (Utility.GetCurrentEvent() == Constants.CornEvent.SHARED_SHUCKING)
                    result.CornCount += Math.Min(Dailies, Constants.SHARED_SHUCKING_MAX_BONUS);
                await GuildTracker.Serializer.AddUser(result);
            }
            return result;
        }

        public async Task<UserInfo> LookupUser(IUser user)
        {
            return await LookupUser(user.Id);
        }

        public async Task<bool> UserExists(IUser user)
        {
            return await GuildTracker.Serializer.UserExists(this, user.Id);
        }

        public async Task<long> GetTotalCorn()
        {
            var users = await GuildTracker.Serializer.GetAllUsers(this);
            return users.Sum(u => u.CornCount);
        }

        public async Task AddCornToAll(long amount, UserInfo? except = null)
        {
            await GuildTracker.Serializer.AddCornToAllUsers(this, amount, except?.UserId ?? 0);
        }

        public async Task<List<UserInfo>> GetLeaderboards(int count = 10)
        {
            var leaderboard = await GuildTracker.Serializer.GetLeaderboards(this);
            return leaderboard.Take(count).ToList();
        }

        public async Task Save()
        {
            await GuildTracker.SaveGuildInfo(this);
        }

        public async Task<string> GetLeaderboardsString(int count = 10, bool addSuffix = true)
        {
            var leaderboards = await GetLeaderboards(count);

            var currencyName = Utility.GetCurrentName();
            var response = new StringBuilder();
            long lastCornAmount = 0;
            int lastPlacementNumber = 0;

            for (int i = 0; i < leaderboards.Count; i++)
            {
                var userInfo = leaderboards[i];

                int placement = i + 1;
                if (userInfo.CornCount == lastCornAmount)
                {
                    placement = lastPlacementNumber;
                }
                else
                {
                    lastPlacementNumber = placement;
                }

                var stringId = userInfo.DiscordUser == null ?
                    userInfo.UserId.ToString() :
                    Utility.GetUserDisplayString(userInfo.DiscordUser, true);
                
                var suffix = (!addSuffix) || userInfo.HasClaimedDaily ?
                    "" : $" {Constants.CALENDAR_EMOJI}";

                response.AppendLine($"{placement} : {stringId} - {userInfo.CornCount} {currencyName}{suffix}");

                lastCornAmount = userInfo.CornCount;
            }

            return response.ToString();
        }

        public async Task SendMonthlyRecap()
        {
            // TODO: implement

            // no announcements channel has been set
            if (AnnouncementChannel == 0) return;

            var client = _services.GetRequiredService<DiscordSocketClient>();
            var guild = client.GetGuild(GuildId);
            var channel = guild?.GetChannel(AnnouncementChannel);
            // the specified channel could not be found in the guild
            if (channel == null) return;
            if (channel is not ITextChannel textChannel) return;

            var users = await GuildTracker.Serializer.GetAllUsers(this);
            var leaderboards = await GetLeaderboardsString(3, false);
            var serverShucks = GetTotalCorn();
            var globalShucks = GuildTracker.GetTotalCorn();

            UserHistory? bestDaily = null;
            string? bestDailyName = null;
            UserHistory? worstDaily = null;
            string? worstDailyName = null;
            UserHistory? bestGambling = null;
            string? bestGamblingName = null;
            UserHistory? worstGambling = null;
            string? worstGamblingName = null;

            foreach (UserInfo user in users)
            {
                var userHistory = await GuildTracker.GetHistory(user.UserId);

                // for statistical significance
                if (userHistory.GetDailyCount(GuildId) >= 15)
                {
                    if (bestDaily == null ||
                    userHistory.GetDailyAverage(GuildId) > bestDaily.GetDailyAverage(GuildId))
                    {
                        IUser userObj = guild?.GetUser(user.UserId) ?? await client.GetUserAsync(user.UserId);
                        if (userObj != null)
                        {
                            bestDaily = userHistory;
                            bestDailyName = Utility.GetUserDisplayString(userObj, true);
                        }
                    }

                    if (worstDaily == null ||
                        userHistory.GetDailyAverage(GuildId) < worstDaily.GetDailyAverage(GuildId))
                    {
                        // TODO: remove nasty copied code
                        IUser userObj = guild?.GetUser(user.UserId) ?? await client.GetUserAsync(user.UserId);
                        if (userObj != null)
                        {
                            worstDaily = userHistory;
                            worstDailyName = Utility.GetUserDisplayString(userObj, true);
                        }
                    }
                }

                if (userHistory.GetNumberOfCornucopias(GuildId) >= 21)
                {
                    if (bestGambling == null ||
                    userHistory.GetCornucopiaPercent(GuildId) > bestGambling.GetCornucopiaPercent(GuildId))
                    {
                        // TODO: remove nasty copied code
                        IUser userObj = guild?.GetUser(user.UserId) ?? await client.GetUserAsync(user.UserId);
                        if (userObj != null)
                        {
                            bestGambling = userHistory;
                            bestGamblingName = Utility.GetUserDisplayString(userObj, true);
                        }
                    }

                    if (worstGambling == null ||
                        userHistory.GetCornucopiaPercent(GuildId) < worstGambling.GetCornucopiaPercent(GuildId))
                    {
                        // TODO: remove nasty copied code
                        IUser userObj = guild?.GetUser(user.UserId) ?? await client.GetUserAsync(user.UserId);
                        if (userObj != null)
                        {
                            worstGambling = userHistory;
                            worstGamblingName = Utility.GetUserDisplayString(userObj, true);
                        }
                    }
                }

            }

            var response = new StringBuilder();
            response.AppendLine($"A total of {serverShucks:n0} corn was shucked in this server, out of {globalShucks:n0} globally!");
            response.AppendLine($"");
            response.AppendLine($"Top 3 shuckers:");
            response.AppendLine(leaderboards);
            if (bestDailyName != null && bestDaily != null)
            {
                response.AppendLine($"The most lucky shucker in the server was {bestDailyName} with a daily average of {bestDaily.GetDailyAverage(GuildId):n2}!");
                response.AppendLine($"");
            }
            if (worstDailyName != null && worstDaily != null)
            {
                response.AppendLine($"Unfortunately, there was also {worstDailyName} with a daily average of {worstDaily.GetDailyAverage(GuildId):n2}.");
                response.AppendLine($"");
            }
            if (bestGamblingName != null && bestGambling != null)
            {
                response.AppendLine($"The best gambler in the server was {bestGamblingName} with a return of " +
                    $"{bestGambling.GetCornucopiaReturns(GuildId):n0} ({bestGambling.GetCornucopiaPercent(GuildId)*100.0:n2}%)!");
                response.AppendLine($"");
            }
            if (worstGamblingName != null && worstGambling != null)
            {
                response.AppendLine($"Unfortunately, there was also {worstGamblingName} with a return of " +
                    $"{worstGambling.GetCornucopiaReturns(GuildId):n0} ({worstGambling.GetCornucopiaPercent(GuildId)*100.0:n2}%).");
            }

            var embed = new EmbedBuilder()
                .WithColor(Color.Gold)
                .WithThumbnailUrl(Constants.CORN_THUMBNAIL_URL)
                .WithTitle($"{Constants.CORN_EMOJI} Monthly recap {Constants.CORN_EMOJI}")
                .WithDescription(response.ToString())
                .WithCurrentTimestamp()
                .Build();

            try { await textChannel.SendMessageAsync(embeds: new Embed[] { embed }); }
            catch (HttpException) { }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(GuildId, GuildTracker.Serializer);
        }

        public override bool Equals(object? obj)
        {
            return obj is GuildInfo other &&
                GuildId == other.GuildId &&
                GuildTracker.Serializer.Equals(other.GuildTracker.Serializer);
        }

    }
}
