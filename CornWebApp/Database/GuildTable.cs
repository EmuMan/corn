using CornWebApp.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CornWebApp.Database
{
    public class GuildTable(SqlConnection connection) : SqlTable<Guild>(connection)
    {
        public override async Task CreateTableIfNotExistAsync()
        {
            using var command = Connection.CreateCommand();
            command.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Guilds' AND type = 'U')
                    BEGIN
                        CREATE TABLE Guilds(
                            GuildId BIGINT NOT NULL,
                            DailyCount INT NOT NULL,
                            AnnouncementChannel BIGINT NOT NULL,
                            CONSTRAINT PK_Guilds PRIMARY KEY (GuildId)
                        );
                    END;";
            await command.ExecuteNonQueryAsync();
        }

        private static Guild GetGuildFromDataReader(SqlDataReader reader)
        {
            return new Guild(
                isNew: false,
                guildId: (ulong)reader.GetInt64(0),
                dailyCount: reader.GetInt32(1),
                announcementChannel: (ulong)reader.GetInt64(2)
            );
        }

        public override async Task InsertAsync(Guild guild)
        {
            var statement = @"
                INSERT INTO Guilds (GuildId, DailyCount, AnnouncementChannel)
                VALUES (@GuildId, @DailyCount, @AnnouncementChannel);";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@GuildId", guild.GuildId, SqlDbType.BigInt),
                BuildSqlParameter("@DailyCount", guild.DailyCount, SqlDbType.Int),
                BuildSqlParameter("@AnnouncementChannel", guild.AnnouncementChannel, SqlDbType.BigInt)
            };
            using var command = new SqlCommand(statement, Connection);
            command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<Guild?> GetAsync(ulong guildId)
        {
            var statement = @"
                SELECT * FROM Guilds
                WHERE GuildId = @GuildId;";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@GuildId", guildId, SqlDbType.BigInt)
            };
            using var reader = await GetDataReaderAsync(statement, parameters);
            if (await reader.ReadAsync())
            {
                return GetGuildFromDataReader(reader);
            }
            return null;
        }

        public async Task<List<Guild>> GetAsync()
        {
            var statement = @"
                SELECT * FROM Guilds;";
            using var reader = await GetDataReaderAsync(statement, []);
            var guilds = new List<Guild>();
            while (await reader.ReadAsync())
            {
                guilds.Add(GetGuildFromDataReader(reader));
            }
            return guilds;
        }

        // Note: This method does not add the new user model to the database.
        // This is by design.
        public async Task<Guild> GetOrCreateAsync(ulong guildId)
        {
            var user = await GetAsync(guildId) ?? new Guild(isNew: true, guildId);
            return user;
        }

        public override async Task UpdateAsync(Guild guild)
        {
            var statement = @"
                UPDATE Guilds
                SET DailyCount = @DailyCount,
                    AnnouncementChannel = @AnnouncementChannel
                WHERE GuildId = @GuildId;";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@GuildId", guild.GuildId, SqlDbType.BigInt),
                BuildSqlParameter("@DailyCount", guild.DailyCount, SqlDbType.Int),
                BuildSqlParameter("@AnnouncementChannel", guild.AnnouncementChannel, SqlDbType.BigInt)
            };
            using var command = new SqlCommand(statement, Connection);
            command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync();
        }

        public async Task InsertOrUpdateAsync(Guild guild)
        {
            if (guild.IsNew)
            {
                await InsertAsync(guild);
            }
            else
            {
                await UpdateAsync(guild);
            }
        }

        public override async Task DeleteAsync(Guild guild)
        {
            var statement = @"
                DELETE FROM Guilds
                WHERE GuildId = @GuildId;";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@GuildId", guild.GuildId, SqlDbType.BigInt)
            };
            using var command = new SqlCommand(statement, Connection);
            command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync();
        }

        public async Task ResetAllDailiesAsync(Guild guild)
        {
            var statement = @"
                UPDATE Users
                SET HasClaimedDaily = 0
                WHERE GuildId = @GuildId;
                UPDATE Guilds
                SET DailyCount = 0
                WHERE GuildId = @GuildId;";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@GuildId", guild.GuildId, SqlDbType.BigInt)
            };
            using var command = new SqlCommand(statement, Connection);
            command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync();
        }

        public async Task ResetAllCornucopiasAsync(Guild guild)
        {
            var statement = @"
                UPDATE Users
                SET CornucopiaCount = 0
                WHERE GuildId = @GuildId;";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@GuildId", guild.GuildId, SqlDbType.BigInt)
            };
            using var command = new SqlCommand(statement, Connection);
            command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync();
        }
    }
}
