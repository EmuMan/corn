using CornWebApp.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace CornWebApp.Connections
{
    public class Database
    {
        SqlConnection Connection;

        public Database(string connectionString)
        {
            Connection = new SqlConnection(connectionString);
            Connection.Open();
        }

        public void Close()
        {
            Connection.Close();
        }

        async public Task CreateTablesIfNotExistAsync()
        {
            using (var command = Connection.CreateCommand())
            {
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

            using (var command = Connection.CreateCommand())
            {
                command.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users' AND type = 'U')
                    BEGIN
                        CREATE TABLE Users(
                            UserId BIGINT NOT NULL,
                            GuildId BIGINT NOT NULL,
                            CornCount BIGINT NOT NULL,
                            HasClaimedDaily INT NOT NULL,
                            CornucopiaCount INT NOT NULL,
                            CornMultiplier FLOAT NOT NULL,
                            CornMultiplierLastEdit BIGINT NOT NULL,
                            CONSTRAINT PK_Users PRIMARY KEY (UserId, GuildId),
                            CONSTRAINT FK_Users_Guilds FOREIGN KEY (GuildId) REFERENCES Guilds(GuildId)
                        );
                    END;";
                await command.ExecuteNonQueryAsync();
            }

            using (var command = Connection.CreateCommand())
            {
                command.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'History' AND type = 'U')
                    BEGIN
                        CREATE TABLE History(
                            Id INT IDENTITY(1,1) NOT NULL,
                            UserId BIGINT NOT NULL,
                            GuildId BIGINT NOT NULL,
                            Type INT NOT NULL,
                            Value BIGINT NOT NULL,
                            Timestamp DATETIME NOT NULL,
                            CONSTRAINT PK_History PRIMARY KEY (Id),
                            CONSTRAINT FK_History_Users FOREIGN KEY (UserId, GuildId) REFERENCES Users(UserId, GuildId)
                        );
                    END;";
                await command.ExecuteNonQueryAsync();
            }
        }

        private SqlParameter BuildSqlParameter(string name, object value, SqlDbType type)
        {
            return new SqlParameter(name, type)
            {
                Value = value
            };
        }

        async private Task<SqlDataReader> GetDataReaderAsync(string statement, SqlParameter[] parameters)
        {
            using var command = new SqlCommand(statement, Connection);
            command.Parameters.AddRange(parameters);
            return await command.ExecuteReaderAsync();
        }

        private User GetUserFromDataReader(SqlDataReader reader)
        {
            return new User(
                isNew: false,
                userId: (ulong)reader.GetInt64(0),
                guildId: (ulong)reader.GetInt64(1),
                cornCount: reader.GetInt64(2),
                hasClaimedDaily: reader.GetInt32(3) == 0 ? false : true,
                cornucopiaCount: reader.GetInt32(4),
                cornMultiplier: reader.GetDouble(5),
                cornMultiplierLastEdit: (ulong)reader.GetInt64(6)
            );
        }

        async public Task InsertUserAsync(User user)
        {
            var statement = @"
                INSERT INTO Users (UserId, GuildId, CornCount, HasClaimedDaily, CornMultiplier, CornMultiplierLastEdit)
                VALUES (@UserId, @GuildId, @CornCount, @HasClaimedDaily, @CornMultiplier, @CornMultiplierLastEdit);";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@UserId", user.UserId, SqlDbType.BigInt),
                BuildSqlParameter("@GuildId", user.GuildId, SqlDbType.BigInt),
                BuildSqlParameter("@CornCount", user.CornCount, SqlDbType.BigInt),
                BuildSqlParameter("@HasClaimedDaily", user.HasClaimedDaily, SqlDbType.Int),
                BuildSqlParameter("@CornMultiplier", user.CornMultiplier, SqlDbType.Float),
                BuildSqlParameter("@CornMultiplierLastEdit", user.CornMultiplierLastEdit, SqlDbType.BigInt)
            };
            using var command = new SqlCommand(statement, Connection);
            command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync();
        }

        async public Task<List<User>> GetUsersFromUserIdAsync(ulong userId)
        {
            var statement = @"
                SELECT * FROM Users
                WHERE UserId = @UserId;";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@UserId", userId, SqlDbType.BigInt)
            };
            using var reader = await GetDataReaderAsync(statement, parameters);
            var users = new List<User>();
            while (await reader.ReadAsync())
            {
                users.Add(GetUserFromDataReader(reader));
            }
            return users;
        }

        async public Task<List<User>> GetUsersFromGuildIdAsync(ulong guildId)
        {
            var statement = @"
                SELECT * FROM Users
                WHERE GuildId = @GuildId;";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@GuildId", guildId, SqlDbType.BigInt)
            };
            using var reader = await GetDataReaderAsync(statement, parameters);
            var users = new List<User>();
            while (await reader.ReadAsync())
            {
                users.Add(GetUserFromDataReader(reader));
            }
            return users;
        }

        async public Task<User?> GetUserAsync(ulong guildId, ulong userId)
        {
            var statement = @"
                SELECT * FROM Users
                WHERE GuildId = @GuildId AND UserId = @UserId";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@GuildId", guildId, SqlDbType.BigInt),
                BuildSqlParameter("@UserId", userId, SqlDbType.BigInt)
            };
            using var reader = await GetDataReaderAsync(statement, parameters);
            if (await reader.ReadAsync())
            {
                return GetUserFromDataReader(reader);
            }
            return null;
        }

        // Note: This method does not add the new user model to the database.
        // This is by design.
        async public Task<User> GetOrCreateUserAsync(ulong guildId, ulong userId)
        {
            var user = await GetUserAsync(guildId, userId) ?? new User(isNew: true, guildId, userId);
            return user;
        }

        async public Task UpdateUserAsync(User user)
        {
            var statement = @"
                UPDATE Users
                SET CornCount = @CornCount,
                    HasClaimedDaily = @HasClaimedDaily,
                    CornMultiplier = @CornMultiplier,
                    CornMultiplierLastEdit = @CornMultiplierLastEdit
                WHERE UserId = @UserId AND GuildId = @GuildId;";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@UserId", user.UserId, SqlDbType.BigInt),
                BuildSqlParameter("@GuildId", user.GuildId, SqlDbType.BigInt),
                BuildSqlParameter("@CornCount", user.CornCount, SqlDbType.BigInt),
                BuildSqlParameter("@HasClaimedDaily", user.HasClaimedDaily, SqlDbType.Int),
                BuildSqlParameter("@CornMultiplier", user.CornMultiplier, SqlDbType.Float),
                BuildSqlParameter("@CornMultiplierLastEdit", user.CornMultiplierLastEdit, SqlDbType.BigInt)
            };
            using var command = new SqlCommand(statement, Connection);
            command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync();
        }

        public async Task InsertOrUpdateUserAsync(User user)
        {
            if (user.IsNew)
            {
                await InsertUserAsync(user);
            }
            else
            {
                await UpdateUserAsync(user);
            }
        }

        async public Task InsertGuildAsync(Guild guild)
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

        async public Task<Guild?> GetGuildAsync(ulong guildId)
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
                return new Guild(
                    isNew: false,
                    guildId: (ulong)reader.GetInt64(0),
                    dailyCount: reader.GetInt32(1),
                    announcementChannel: (ulong)reader.GetInt64(2)
                );
            }
            return null;
        }

        // Note: This method does not add the new user model to the database.
        // This is by design.
        async public Task<Guild> GetOrCreateGuildAsync(ulong guildId)
        {
            var user = await GetGuildAsync(guildId) ?? new Guild(isNew: true, guildId);
            return user;
        }

        public async Task UpdateGuildAsync(Guild guild)
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

        public async Task InsertOrUpdateGuildAsync(Guild guild)
        {
            if (guild.IsNew)
            {
                await InsertGuildAsync(guild);
            }
            else
            {
                await UpdateGuildAsync(guild);
            }
        }

        async public Task ResetDailyAsync(User user)
        {
            var statement = @"
                UPDATE Users
                SET HasClaimedDaily = 0
                WHERE GuildId = @GuildId AND UserId = @UserId;";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@GuildId", user.GuildId, SqlDbType.BigInt),
                BuildSqlParameter("@UserId", user.UserId, SqlDbType.BigInt)
            };
            using var command = new SqlCommand(statement, Connection);
            command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync();
        }

        async public Task ResetAllDailiesAsync(Guild guild)
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

        async public Task ResetAllDailiesAsync()
        {
            var statement = @"
                UPDATE Users
                SET HasClaimedDaily = 0;
                UPDATE Guilds
                SET DailyCount = 0;";
            using var command = new SqlCommand(statement, Connection);
            await command.ExecuteNonQueryAsync();
        }

        async public Task ResetCornucopiaAsync(User user)
        {
            var statement = @"
                UPDATE Users
                SET CornucopiaCount = 0
                WHERE GuildId = @GuildId AND UserId = @UserId;";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@GuildId", user.GuildId, SqlDbType.BigInt),
                BuildSqlParameter("@UserId", user.UserId, SqlDbType.BigInt)
            };
            using var command = new SqlCommand(statement, Connection);
            command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync();
        }

        async public Task ResetAllCornucopiasAsync(Guild guild)
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

        async public Task ResetAllCornucopiasAsync()
        {
            var statement = @"
                UPDATE Users
                SET CornucopiaCount = 0;";
            using var command = new SqlCommand(statement, Connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
