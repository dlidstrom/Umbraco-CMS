using System.Data.Common;
using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.FaultHandling;
using Umbraco.Cms.Persistence.Postgresql.Services;

namespace Umbraco.Cms.Persistence.Postgresql.Interceptors;

public class PostgresqlAddRetryPolicyInterceptor : PostgresqlConnectionInterceptor
{
    public override DbConnection OnConnectionOpened(IDatabase database, DbConnection conn)
    {
        RetryStrategy retryStrategy = RetryStrategy.DefaultExponential;
        var commandRetryPolicy = new RetryPolicy(new PostgresqlTransientErrorDetectionStrategy(), retryStrategy);
        return new RetryDbConnection(conn, null, commandRetryPolicy);
    }
}
