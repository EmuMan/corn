using CornWebApp.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CornWebApp.Database
{
    public class UserTable(SqlConnection connection) : SqlTable<User>(connection)
    {
        public override async Task CreateTableIfNotExistAsync()
        {
            using var command = Connection.CreateCommand();
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

        private static User GetUserFromDataReader(SqlDataReader reader)
        {
            return new User(
                isNew: false,
                userId: (ulong)reader.GetInt64(0),
                guildId: (ulong)reader.GetInt64(1),
                cornCount: reader.GetInt64(2),
                hasClaimedDaily: reader.GetInt32(3) != 0,
                cornucopiaCount: reader.GetInt32(4),
                cornMultiplier: reader.GetDouble(5),
                cornMultiplierLastEdit: (ulong)reader.GetInt64(6)
            );
        }

        public override async Task InsertAsync(User user)
        {
            var statement = @"
                INSERT INTO Users (GuildId, UserId, CornCount, HasClaimedDaily, CornucopiaCount, CornMultiplier, CornMultiplierLastEdit)
                VALUES (@GuildId, @UserId, @CornCount, @HasClaimedDaily, @CornucopiaCount, @CornMultiplier, @CornMultiplierLastEdit);";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@GuildId", user.GuildId, SqlDbType.BigInt),
                BuildSqlParameter("@UserId", user.UserId, SqlDbType.BigInt),
                BuildSqlParameter("@CornCount", user.CornCount, SqlDbType.BigInt),
                BuildSqlParameter("@HasClaimedDaily", user.HasClaimedDaily, SqlDbType.Int),
                BuildSqlParameter("@CornucopiaCount", user.CornucopiaCount, SqlDbType.Int),
                BuildSqlParameter("@CornMultiplier", user.CornMultiplier, SqlDbType.Float),
                BuildSqlParameter("@CornMultiplierLastEdit", user.CornMultiplierLastEdit, SqlDbType.BigInt)
            };
            using var command = new SqlCommand(statement, Connection);
            command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<User>> GetFromUserIdAsync(ulong userId)
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

        public async Task<List<User>> GetFromGuildIdAsync(ulong guildId)
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

        public async Task<User?> GetAsync(ulong guildId, ulong userId)
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

        public async Task<List<User>> GetAsync()
        {
            var statement = @"
                SELECT * FROM Users;";
            using var reader = await GetDataReaderAsync(statement, []);
            var users = new List<User>();
            while (await reader.ReadAsync())
            {
                users.Add(GetUserFromDataReader(reader));
            }
            return users;
        }

        // Note: This method does not add the new user model to the database.
        // This is by design.
        public async Task<User> GetOrCreateAsync(ulong guildId, ulong userId)
        {
            var user = await GetAsync(guildId, userId) ?? new User(isNew: true, guildId, userId);
            return user;
        }

        public override async Task UpdateAsync(User user)
        {
            var statement = @"
                UPDATE Users
                SET CornCount = @CornCount,
                    HasClaimedDaily = @HasClaimedDaily,
                    CornucopiaCount = @CornucopiaCount,
                    CornMultiplier = @CornMultiplier,
                    CornMultiplierLastEdit = @CornMultiplierLastEdit
                WHERE GuildId = @GuildId AND UserId = @UserId;";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@GuildId", user.GuildId, SqlDbType.BigInt),
                BuildSqlParameter("@UserId", user.UserId, SqlDbType.BigInt),
                BuildSqlParameter("@CornCount", user.CornCount, SqlDbType.BigInt),
                BuildSqlParameter("@HasClaimedDaily", user.HasClaimedDaily, SqlDbType.Int),
                BuildSqlParameter("@CornucopiaCount", user.CornucopiaCount, SqlDbType.Int),
                BuildSqlParameter("@CornMultiplier", user.CornMultiplier, SqlDbType.Float),
                BuildSqlParameter("@CornMultiplierLastEdit", user.CornMultiplierLastEdit, SqlDbType.BigInt)
            };
            using var command = new SqlCommand(statement, Connection);
            command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync();
        }

        public async Task InsertOrUpdateAsync(User user)
        {
            if (user.IsNew)
            {
                await InsertAsync(user);
            }
            else
            {
                await UpdateAsync(user);
            }
        }

        public override async Task DeleteAsync(User user)
        {
            var statement = @"
                DELETE FROM Users
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

        public async Task ResetDailyAsync(User user)
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

        public async Task ResetCornucopiaAsync(User user)
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

        public async Task<List<User>> GetLeaderboardsAsync(Guild guild, int limit)
        {
            var statement = @"
                SELECT TOP (@Limit) *
                FROM Users
                WHERE GuildId = @GuildId
                ORDER BY CornCount DESC;";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@Limit", limit, SqlDbType.Int),
                BuildSqlParameter("@GuildId", guild.GuildId, SqlDbType.BigInt)
            };
            using var reader = await GetDataReaderAsync(statement, parameters);
            var users = new List<User>();
            while (await reader.ReadAsync())
            {
                users.Add(GetUserFromDataReader(reader));
            }
            return users;
        }

        public async Task<List<User>> GetLeaderboardsAsync(int limit)
        {
            var statement = @"
                SELECT TOP (@Limit) *
                FROM Users
                ORDER BY CornCount DESC;";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@Limit", limit, SqlDbType.Int)
            };
            using var reader = await GetDataReaderAsync(statement, parameters);
            var users = new List<User>();
            while (await reader.ReadAsync())
            {
                users.Add(GetUserFromDataReader(reader));
            }
            return users;
        }

        public async Task<List<Tuple<ulong, long>>> GetTotalsAsync(ulong userId)
        {
            var statement = @"
                SELECT GuildId, SUM(CornCount) AS Total
                FROM Users
                WHERE UserId = @UserId
                GROUP BY GuildId;";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@UserId", userId, SqlDbType.BigInt)
            };
            using var reader = await GetDataReaderAsync(statement, parameters);
            var totals = new List<Tuple<ulong, long>>();
            while (await reader.ReadAsync())
            {
                totals.Add(new Tuple<ulong, long>((ulong)reader.GetInt64(0), reader.GetInt64(1)));
            }
            return totals;
        }
    }
}
