using Npgsql;
using Umbraco.Cms.Infrastructure.Persistence;

namespace Umbraco.Cms.Persistence.Postgresql.Services;

/// <summary>
///     Implements <see cref="IDatabaseCreator" /> for PostgreSQL.
/// </summary>
public class PostgresqlDatabaseCreator : IDatabaseCreator
{
    /// <inheritdoc />
    public string ProviderName => Constants.ProviderName;

    public void Create(string connectionString)
    {
        NpgsqlConnectionStringBuilder builder = new(connectionString);
        string? database = builder.Database;
        builder.Database = string.Empty;
        using NpgsqlConnection conn = new(builder.ConnectionString);
        conn.Open();

        object? r = Query(
            "SELECT datname FROM pg_database WHERE datname = @database",
            cmd => cmd.Parameters.Add("database", NpgsqlTypes.NpgsqlDbType.Varchar).Value = database);
        if (r is null)
        {
            _ = Query($"CREATE DATABASE {database}");
        }

        object? Query(string sql, Action<NpgsqlCommand>? action = null)
        {
            NpgsqlCommand cmd = new(sql, conn);
            action?.Invoke(cmd);
            return cmd.ExecuteScalar();
        }
    }
}
