using System.Data.Common;
using NPoco;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;

namespace Umbraco.Cms.Persistence.Postgresql.Interceptors;

public class PostgresqlAddMiniProfilerInterceptor : PostgresqlConnectionInterceptor
{
    public override DbConnection OnConnectionOpened(IDatabase database, DbConnection conn)
        => new ProfiledDbConnection(conn, MiniProfiler.Current);
}
