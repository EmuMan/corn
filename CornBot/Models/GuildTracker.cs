using CornBot.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CornBot.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using CornBot.Utilities;

namespace CornBot.Models
{
    public class GuildTracker
    {

        public GuildTrackerSerializer Serializer { get; private set; }
        private readonly IServiceProvider _services;

        public GuildTracker(GuildTrackerSerializer serializer, IServiceProvider services)
        {
            Serializer = serializer;
            _services = services;
        }

        public async Task<GuildInfo> LookupGuild(ulong guildId)
        {
            var result = await Serializer.GetGuild(this, guildId);
            if (result == null)
            {
                result = new(this, guildId, 0, 0, _services);
                await Serializer.AddGuild(result);
            }
            return result;
        }

        public async Task<GuildInfo> LookupGuild(SocketGuild guild)
        {
            return await LookupGuild(guild.Id);
        }

        public async Task<long> GetTotalCorn()
        {
            // don't think you can do an async sum
            long sum = 0;
            foreach (var guild in await Serializer.GetAllGuilds(this))
                sum += await guild.GetTotalCorn();
            return sum;
        }

        public long GetTotalCorn(IUser user)
        {
            // TODO: implement
        }

        public async Task ResetDailies()
        {
            await Serializer.ResetAllDailies();
        }

        public async Task SendAllMonthlyRecaps()
        {
            foreach (var guild in await Serializer.GetAllGuilds(this))
                await guild.SendMonthlyRecap();
        }

        public async Task AddCornToAll(long amount)
        {
            await Serializer.AddCornToAllUsers(amount);
        }

        public async Task StartDailyResetLoop()
        {
            var client = _services.GetRequiredService<CornClient>();

            var lastReset = Utility.GetAdjustedTimestamp();
            var nextReset = lastReset.AddDays(1);
            nextReset = new(nextReset.Year, nextReset.Month, nextReset.Day, hour: 0, minute: 0, second: 0, Constants.TZ_OFFSET);
            while (true)
            {
                // wait until the next day
                var timeUntilReset = nextReset - Utility.GetAdjustedTimestamp();
                await client.Log(new LogMessage(LogSeverity.Info, "DailyReset",
                    $"Time until next reset: {timeUntilReset}"));
                await Task.Delay(timeUntilReset);

                // create a backup (with date info corresponding to the previous day)
                await Serializer.BackupDatabase($"./backups/{lastReset.Year}/{lastReset.Month}/backup-{lastReset.Day}.db");

                // either reset dailies or the entire leaderboard (depending on whether end of month)
                if (lastReset.Month == nextReset.Month)
                {
                    await ResetDailies();
                    await client.Log(new LogMessage(LogSeverity.Info, "DailyReset", "Daily reset performed successfully!"));
                }
                else
                {
                    await SendAllMonthlyRecaps();
                    
                    await Serializer.ClearDatabase();
                    Guilds = new();
                    await client.Log(new LogMessage(LogSeverity.Info, "DailyReset", "Monthly reset performed successfully!"));
                    await client.Log(new LogMessage(LogSeverity.Info, "DailyReset", "CORN HAS BEEN RESET FOR THE MONTH!"));
                }
                
                // update next and last reset in lockstep
                lastReset = nextReset;
                nextReset = nextReset.AddDays(1);
            }
        }

        public async Task SaveUserInfo(UserInfo user)
        {
            await Serializer.AddOrUpdateGuild(user.Guild);
            await Serializer.AddOrUpdateUser(user);
        }

        public async Task SaveGuildInfo(GuildInfo guild)
        {
            await Serializer.AddOrUpdateGuild(guild);
        }

        public async Task LogAction(UserInfo user, UserHistory.ActionType type, long value)
        {
            await Serializer.LogAction(user, type, value, Utility.GetAdjustedTimestamp());
        }

        public async Task<UserHistory> GetHistory(ulong userId)
        {
            return await Serializer.GetHistory(userId);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Serializer);
        }

        public override bool Equals(object? obj)
        {
            return obj is GuildTracker other && Serializer == other.Serializer;
        }

    }
}
