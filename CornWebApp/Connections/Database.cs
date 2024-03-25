﻿using CornWebApp.Models;
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
                userId: (ulong)reader.GetInt64(0),
                guildId: (ulong)reader.GetInt64(1),
                cornCount: reader.GetInt64(2),
                hasClaimedDaily: reader.GetInt32(3) == 0 ? false : true,
                cornMultiplier: reader.GetDouble(4),
                cornMultiplierLastEdit: (ulong)reader.GetInt64(5)
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

        async public Task<User?> GetUserAsync(ulong userId, ulong guildId)
        {
            var statement = @"
                SELECT * FROM Users
                WHERE UserId = @UserId AND GuildId = @GuildId";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@UserId", userId, SqlDbType.BigInt),
                BuildSqlParameter("@GuildId", guildId, SqlDbType.BigInt)
            };
            using var reader = await GetDataReaderAsync(statement, parameters);
            if (await reader.ReadAsync())
            {
                return GetUserFromDataReader(reader);
            }
            return null;
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
                    guildId: (ulong)reader.GetInt64(0),
                    dailyCount: reader.GetInt32(1),
                    announcementChannel: (ulong)reader.GetInt64(2)
                );
            }
            return null;
        }
    }
}
