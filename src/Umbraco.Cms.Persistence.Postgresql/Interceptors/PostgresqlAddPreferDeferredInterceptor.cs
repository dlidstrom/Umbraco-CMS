using System.Data.Common;
using Npgsql;
using NPoco;
using Umbraco.Cms.Persistence.Postgresql.Services;

namespace Umbraco.Cms.Persistence.Postgresql.Interceptors;

public class PostgresqlAddPreferDeferredInterceptor : PostgresqlConnectionInterceptor
{
    public override DbConnection OnConnectionOpened(IDatabase database, DbConnection conn)
        => new PostgresqlPreferDeferredTransactionsConnection(
            conn as NpgsqlConnection ?? throw new InvalidOperationException());
}
