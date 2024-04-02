using CornWebApp.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CornWebApp.Database
{
    public class HistoryTable(SqlConnection connection) : SqlTable<HistoryEntry>(connection)
    {
        public override async Task CreateTableIfNotExistAsync()
        {
            using var command = Connection.CreateCommand();
            command.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'History' AND type = 'U')
                    BEGIN
                        CREATE TABLE History(
                            Id INT IDENTITY(1,1) NOT NULL,
                            GuildId BIGINT NOT NULL,
                            UserId BIGINT NOT NULL,
                            Type INT NOT NULL,
                            Value BIGINT NOT NULL,
                            Timestamp VARCHAR(40) NOT NULL,
                            CONSTRAINT PK_History PRIMARY KEY (Id),
                            CONSTRAINT FK_History_Users FOREIGN KEY (UserId, GuildId) REFERENCES Users(UserId, GuildId)
                        );
                    END;";
            await command.ExecuteNonQueryAsync();
        }

        private static HistoryEntry GetHistoryEntryFromDataReader(SqlDataReader reader)
        {
            return new HistoryEntry(
                id: reader.GetInt32(0),
                guildId: (ulong)reader.GetInt64(1),
                userId: (ulong)reader.GetInt64(2),
                actionType: (HistoryEntry.ActionType)reader.GetInt32(3),
                value: reader.GetInt64(4),
                timestamp: DateTimeOffset.Parse(reader.GetString(5))
            );
        }

        public override async Task InsertAsync(HistoryEntry entry)
        {
            var statement = @"
                INSERT INTO History (GuildId, UserId, Type, Value, Timestamp)
                VALUES (@GuildId, @UserId, @Type, @Value, @Timestamp);";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@GuildId", entry.GuildId, SqlDbType.BigInt),
                BuildSqlParameter("@UserId", entry.UserId, SqlDbType.BigInt),
                BuildSqlParameter("@Type", (int)entry.Type, SqlDbType.Int),
                BuildSqlParameter("@Value", entry.Value, SqlDbType.BigInt),
                BuildSqlParameter("@Timestamp", entry.Timestamp.ToString("o"), SqlDbType.VarChar)
            };
            using var command = new SqlCommand(statement, Connection);
            command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<HistoryEntry>> GetAsync(ulong userId, ulong guildId)
        {
            var statement = @"
                SELECT Id, GuildId, UserId, Type, Value, Timestamp
                FROM History
                WHERE GuildId = @GuildId AND UserId = @UserId;";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@GuildId", guildId, SqlDbType.BigInt),
                BuildSqlParameter("@UserId", userId, SqlDbType.BigInt)
            };
            using var reader = await GetDataReaderAsync(statement, parameters);
            var entries = new List<HistoryEntry>();
            while (await reader.ReadAsync())
            {
                entries.Add(GetHistoryEntryFromDataReader(reader));
            }
            return entries;
        }

        public async Task<List<HistoryEntry>> GetFromUserIdAsync(ulong userId)
        {
            var statement = @"
                SELECT Id, GuildId, UserId, Type, Value, Timestamp
                FROM History
                WHERE UserId = @UserId;";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@UserId", userId, SqlDbType.BigInt)
            };
            using var reader = await GetDataReaderAsync(statement, parameters);
            var entries = new List<HistoryEntry>();
            while (await reader.ReadAsync())
            {
                entries.Add(GetHistoryEntryFromDataReader(reader));
            }
            return entries;
        }

        public async Task<List<HistoryEntry>> GetFromGuildIdAsync(ulong guildId)
        {
            var statement = @"
                SELECT Id, GuildId, UserId, Type, Value, Timestamp
                FROM History
                WHERE GuildId = @GuildId;";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@GuildId", guildId, SqlDbType.BigInt)
            };
            using var reader = await GetDataReaderAsync(statement, parameters);
            var entries = new List<HistoryEntry>();
            while (await reader.ReadAsync())
            {
                entries.Add(GetHistoryEntryFromDataReader(reader));
            }
            return entries;
        }

        public async Task<List<HistoryEntry>> GetAsync()
        {
            var statement = @"
                SELECT Id, GuildId, UserId, Type, Value, Timestamp
                FROM History;";
            using var reader = await GetDataReaderAsync(statement, []);
            var entries = new List<HistoryEntry>();
            while (await reader.ReadAsync())
            {
                entries.Add(GetHistoryEntryFromDataReader(reader));
            }
            return entries;
        }

        public override async Task UpdateAsync(HistoryEntry historyEntry)
        {
            var statement = @"
                UPDATE History
                SET GuildId = @GuildId, UserId = @UserId, Type = @Type, Value = @Value, Timestamp = @Timestamp
                WHERE Id = @Id;";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@GuildId", historyEntry.GuildId, SqlDbType.BigInt),
                BuildSqlParameter("@UserId", historyEntry.UserId, SqlDbType.BigInt),
                BuildSqlParameter("@Type", (int)historyEntry.Type, SqlDbType.Int),
                BuildSqlParameter("@Value", historyEntry.Value, SqlDbType.BigInt),
                BuildSqlParameter("@Timestamp", historyEntry.Timestamp.ToString("o"), SqlDbType.VarChar),
                BuildSqlParameter("@Id", historyEntry.Id, SqlDbType.Int)
            };
            using var command = new SqlCommand(statement, Connection);
            command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync();
        }

        public override async Task DeleteAsync(HistoryEntry historyEntry)
        {
            var statement = @"
                DELETE FROM History
                WHERE Id = @Id;";
            var parameters = new SqlParameter[]
            {
                BuildSqlParameter("@Id", historyEntry.Id, SqlDbType.Int)
            };
            using var command = new SqlCommand(statement, Connection);
            command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync();
        }
    }
}
