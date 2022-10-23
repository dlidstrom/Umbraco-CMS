using System.Data.Common;
using NPoco;
using Umbraco.Cms.Infrastructure.Persistence;

namespace Umbraco.Cms.Persistence.Postgresql.Interceptors;

public abstract class PostgresqlConnectionInterceptor : IProviderSpecificConnectionInterceptor
{
    public string ProviderName => Constants.ProviderName;

    public abstract DbConnection OnConnectionOpened(IDatabase database, DbConnection conn);

    public virtual void OnConnectionClosing(IDatabase database, DbConnection conn)
    {
    }
}
