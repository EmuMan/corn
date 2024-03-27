using Microsoft.Data.SqlClient;
using System.Data;

namespace CornWebApp.Database
{
    public class SqlDatabase
    {
        public SqlConnection Connection { get; private set; }

        public UserTable Users { get; private set; }
        public GuildTable Guilds { get; private set; }
        public HistoryTable History { get; private set; }

        public SqlDatabase(string connectionString)
        {
            Connection = new SqlConnection(connectionString);

            Users = new UserTable(Connection);
            Guilds = new GuildTable(Connection);
            History = new HistoryTable(Connection);

            Connection.Open();
        }

        public void Close()
        {
            Connection.Close();
        }

        public async Task CreateTablesIfNotExistAsync()
        {
            await Users.CreateTableIfNotExistAsync();
            await Guilds.CreateTableIfNotExistAsync();
            await History.CreateTableIfNotExistAsync();
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
