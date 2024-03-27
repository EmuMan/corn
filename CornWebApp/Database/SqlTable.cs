using Microsoft.Data.SqlClient;
using System.Data;

namespace CornWebApp.Database
{
    public abstract class SqlTable<TRow>(SqlConnection connection)
    {
        public SqlConnection Connection { get; private set; } = connection;

        public abstract Task CreateTableIfNotExistAsync();

        public static SqlParameter BuildSqlParameter(string name, object value, SqlDbType type)
        {
            return new SqlParameter(name, type)
            {
                Value = value
            };
        }

        public async Task<SqlDataReader> GetDataReaderAsync(string statement, SqlParameter[] parameters)
        {
            using var command = new SqlCommand(statement, Connection);
            command.Parameters.AddRange(parameters);
            return await command.ExecuteReaderAsync();
        }

        public abstract Task InsertAsync(TRow row);

        public abstract Task UpdateAsync(TRow row);

        public abstract Task DeleteAsync(TRow row);
    }
}
