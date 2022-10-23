using NPoco;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Persistence.Postgresql.Mappers;

namespace Umbraco.Cms.Persistence.Postgresql.Services;

/// <summary>
///     Implements <see cref="IProviderSpecificMapperFactory" /> for PostgreSQL.
/// </summary>
public class PostgresqlSpecificMapperFactory : IProviderSpecificMapperFactory
{
    /// <inheritdoc />
    public string ProviderName => Constants.ProviderName;

    /// <inheritdoc />
    public NPocoMapperCollection Mappers => new(() => Array.Empty<IMapper>());
}
