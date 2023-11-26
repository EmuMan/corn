using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using CornBot.Models;
using Microsoft.Data.Sqlite;
using Discord.WebSocket;
using Discord;
using Newtonsoft.Json.Linq;
using System.Reactive;
using CornBot.Utilities;
using System.Diagnostics;

namespace CornBot.Serialization
{
    public class GuildTrackerSerializer
    {

        private readonly IServiceProvider _services;
        private SqliteConnection? _connection;

        public GuildTrackerSerializer(IServiceProvider services)
        {
            _services = services;
        }

        public void Initialize(string filename)
        {
            // insecure but all strings are statically supplied
            _connection = new SqliteConnection($"Data Source={filename}");
            _connection.Open();
        }

        public void Close()
        {
            _connection!.Close();
        }

        public async Task<Dictionary<ulong, GuildInfo>> Load(GuildTracker tracker)
        {
            await CreateTablesIfNotExist();

            Dictionary<ulong, GuildInfo> guilds = new();

            using (var gCommand = _connection!.CreateCommand())
            {
                gCommand.CommandText = @"SELECT * FROM guilds";
                var guildIterator = await gCommand.ExecuteReaderAsync();

                while (await guildIterator.ReadAsync())
                {
                    var guildId = (ulong)guildIterator.GetInt64(0);
                    var dailies = guildIterator.GetInt32(1);
                    var announcementChannel = (ulong)guildIterator.GetInt64(2);

                    GuildInfo guild = new(tracker, guildId, dailies, announcementChannel, _services);

                    using (var uCommand = _connection!.CreateCommand())
                    {
                        uCommand.CommandText = @"SELECT * FROM users WHERE guild = @guildId;";
                        uCommand.Parameters.AddWithValue("@guildId", guildId);

                        var userIterator = await uCommand.ExecuteReaderAsync();

                        while (await userIterator.ReadAsync())
                        {
                            var userId = (ulong)userIterator.GetInt64(0);
                            UserInfo user = new(
                                guild: guild,
                                userId: userId,
                                cornCount: userIterator.GetInt64(2),
                                hasClaimedDaily: userIterator.GetInt32(3) != 0,
                                cornMultiplier: userIterator.GetDouble(4),
                                cornMultiplierLastEdit: DateTime.FromBinary(userIterator.GetInt64(5)),
                                _services
                            );
                            guild.AddUserInfo(user);
                        }
                    }

                    guilds.Add(guild.GuildId, guild);
                }
            }

            return guilds;
        }

        public async Task<bool> UserExists(UserInfo user)
        {
            using var command = _connection!.CreateCommand();
            command.CommandText = @"SELECT * FROM users WHERE id = @userId AND guild = @guildId";
            command.Parameters.AddRange(new SqliteParameter[] {
                    new("@userId", user.UserId),
                    new("@guildId", user.Guild.GuildId),
                });
            await using var userIterator = await command.ExecuteReaderAsync();
            if (await userIterator.ReadAsync()) return true;
            return false;
        }

        public async Task<UserInfo?> GetUser(GuildInfo guild, ulong userId)
        {
            using var command = _connection!.CreateCommand();
            command.CommandText = @"SELECT * FROM users WHERE id = @userId AND guild = @guildId";
            command.Parameters.AddRange(new SqliteParameter[] {
                    new("@userId", userId),
                    new("@guildId", guild.GuildId),
                });
            await using var userIterator = await command.ExecuteReaderAsync();
            if (!await userIterator.ReadAsync())
                return null;

            return new(
                guild: guild,
                userId: userId,
                cornCount: userIterator.GetInt64(2),
                hasClaimedDaily: userIterator.GetInt32(3) != 0,
                cornMultiplier: userIterator.GetDouble(4),
                cornMultiplierLastEdit: DateTime.FromBinary(userIterator.GetInt64(5)),
                _services
            );
        }

        public async Task AddUser(UserInfo user)
        {
            using var command = _connection!.CreateCommand();
            command.CommandText = @"
                    INSERT INTO users(id, guild, corn, daily, corn_multiplier, corn_multiplier_last_edit)
                    VALUES(@userId, @guildId, @cornCount, @daily, @cornMultiplier, @cornMultiplierLastEdit )";
            command.Parameters.AddRange(new SqliteParameter[] {
                new("@userId", user.UserId),
                new("@guildId", user.Guild.GuildId),
                new("@cornCount", user.CornCount),
                    new("@daily", user.HasClaimedDaily ? 1 : 0),
                    new("@cornMultiplier", user.CornMultiplier),
                    new("@cornMultiplierLastEdit", user.CornMultiplierLastEdit.ToBinary()),
                });
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateUser(UserInfo user)
        {
            using var command = _connection!.CreateCommand();
            command.CommandText = @"
                    UPDATE users
                    SET corn = @cornCount,
                        daily = @daily,
                        corn_multiplier = @cornMultiplier,
                        corn_multiplier_last_edit = @cornMultiplierLastEdit
                    WHERE id = @userId AND guild = @guildId";
            command.Parameters.AddRange(new SqliteParameter[] {
                    new("@cornCount", user.CornCount),
                    new("@daily", user.HasClaimedDaily ? 1 : 0),
                    new("@cornMultiplier", user.CornMultiplier),
                    new("@cornMultiplierLastEdit", user.CornMultiplierLastEdit.ToBinary()),
                    new("@userId", user.UserId),
                    new("@guildId", user.Guild.GuildId),
                });
            await command.ExecuteNonQueryAsync();
        }

        public async Task AddOrUpdateUser(UserInfo user)
        {
            if (await UserExists(user))
                await UpdateUser(user);
            else
                await AddUser(user);
        }

        public async Task ResetAllDailies()
        {
            using var command = _connection!.CreateCommand();
            command.CommandText = @"
                    UPDATE users
                    SET daily = @daily";
            command.Parameters.AddWithValue("@daily", 0);
            await command.ExecuteNonQueryAsync();
        }
        
        public async Task<bool> GuildExists(GuildInfo guild)
        {
            using var command = _connection!.CreateCommand();
            command.CommandText = @"SELECT * FROM guilds WHERE id = @guildId";
            command.Parameters.AddWithValue("@guildId", guild.GuildId);
            await using var guildIterator = await command.ExecuteReaderAsync();
            if (await guildIterator.ReadAsync()) return true;
            return false;
        }

        public async Task<GuildInfo?> GetGuild(GuildTracker tracker, ulong guildId)
        {
            using var command = _connection!.CreateCommand();
            command.CommandText = @"SELECT * FROM guilds WHERE id = @guildId";
            command.Parameters.AddWithValue("@guildId", guildId);
            await using var guildIterator = await command.ExecuteReaderAsync();
            if (!await guildIterator.ReadAsync())
                return null;

            var dailies = guildIterator.GetInt32(1);
            var announcementChannel = (ulong)guildIterator.GetInt64(2);

            return new(tracker, guildId, dailies, announcementChannel, _services);
        }

        public async Task AddGuild(GuildInfo guild)
        {
            using var command = _connection!.CreateCommand();
            command.CommandText = @"
                    INSERT INTO guilds(id, dailies, announcement_channel)
                    VALUES(@guildId, @dailies, @announcementChannel)";
            command.Parameters.AddRange(new SqliteParameter[] {
                    new("@guildId", guild.GuildId),
                    new("@dailies", guild.Dailies),
                    new("@announcementChannel", guild.AnnouncementChannel),
                });
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateGuild(GuildInfo guild)
        {
            using var command = _connection!.CreateCommand();
            command.CommandText = @"
                    UPDATE guilds
                    SET dailies = @dailies,
                        announcement_channel = @announcementChannel
                    WHERE id = @guildId";
            command.Parameters.AddRange(new SqliteParameter[] {
                    new("@dailies", guild.Dailies),
                    new("@guildId", guild.GuildId),
                    new("@announcementChannel", guild.AnnouncementChannel),
            });
            await command.ExecuteNonQueryAsync();
        }

        public async Task AddOrUpdateGuild(GuildInfo guild)
        {
            if (await GuildExists(guild))
                await UpdateGuild(guild);
            else
                await AddGuild(guild);
        }

        public async Task LogActionRaw(UserHistory.HistoryEntry entry)
        {
            using var command = _connection!.CreateCommand();
            command.CommandText = @"
                    INSERT INTO history(user, guild, type, value, timestamp)
                    VALUES(@userId, @guildId, @type, @value, @timestamp)";
            command.Parameters.AddRange(new SqliteParameter[] {
                    new("@userId", entry.UserId),
                    new("@guildId", entry.GuildId),
                    new("@type", (int)entry.Type),
                    new("@value", entry.Value),
                    new("@timestamp", entry.Timestamp.ToString("o"))
                });
            await command.ExecuteNonQueryAsync();
        }

        public async Task LogAction(UserInfo user, UserHistory.ActionType type, long value, DateTimeOffset timestamp)
        {
            await LogActionRaw(new UserHistory.HistoryEntry {
                UserId = user.UserId,
                GuildId = user.Guild.GuildId,
                Type = type,
                Value = value,
                Timestamp = timestamp,
            });
        }

        public async Task<UserHistory> GetHistory(ulong userId)
        {
            UserHistory history = new(userId);

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"SELECT * FROM history WHERE user = @userId;";
                command.Parameters.AddWithValue("@userId", userId);

                var historyIterator = await command.ExecuteReaderAsync();

                while (await historyIterator.ReadAsync())
                {
                    history.AddAction(new UserHistory.HistoryEntry{
                        Id = (ulong) historyIterator.GetInt64(0),
                        UserId = (ulong) historyIterator.GetInt64(1),
                        GuildId = (ulong)historyIterator.GetInt64(2),
                        Type = (UserHistory.ActionType) historyIterator.GetInt32(3),
                        Value = historyIterator.GetInt64(4),
                        Timestamp = DateTimeOffset.Parse(historyIterator.GetString(5))
                    });
                }
            }

            return history;
        }

        private async Task CreateTablesIfNotExist()
        {
            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS users(
                        [id] INTEGER NOT NULL,
                        [guild] INTEGER NOT NULL,
                        [corn] INTEGER NOT NULL,
                        [daily] INTEGER NOT NULL,
                        [corn_multiplier] REAL NOT NULL,
                        [corn_multiplier_last_edit] INTEGER NOT NULL
                    )";
                await command.ExecuteNonQueryAsync();
            }

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS guilds(
                        [id] INTEGER NOT NULL PRIMARY KEY,
                        [dailies] INTEGER NOT NULL,
                        [announcement_channel] INTEGER NOT NULL
                    )";
                await command.ExecuteNonQueryAsync();
            }

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS history(
                        [id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        [user] INTEGER NOT NULL,
                        [guild] INTEGER NOT NULL,
                        [type] INTEGER NOT NULL,
                        [value] INTEGER NOT NULL,
                        [timestamp] TEXT NOT NULL
                    )";
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task BackupDatabase(string path)
        {
            new FileInfo(path).Directory!.Create();
            var backup = new SqliteConnection($"Data Source={path}");
            _connection!.BackupDatabase(backup);
            await backup.CloseAsync();
        }

        public async Task ClearDatabase()
        {
            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"DELETE FROM users";
                await command.ExecuteNonQueryAsync();
            }

            // have to update guilds differently to avoid resetting announcements channel
            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    UPDATE guilds
                    SET dailies = 0";
                await command.ExecuteNonQueryAsync();
            }            

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"DELETE FROM history";
                await command.ExecuteNonQueryAsync();
            }
        }

    }
}
