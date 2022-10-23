using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.DistributedLocking;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Persistence.SqlSyntax;
using Umbraco.Cms.Persistence.Postgresql.Interceptors;
using Umbraco.Cms.Persistence.Postgresql.Services;

namespace Umbraco.Cms.Persistence.Postgresql;

/// <summary>
///     PostgreSQL support extensions for IUmbracoBuilder.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    ///     Add required services for PostgreSQL support.
    /// </summary>
    public static IUmbracoBuilder AddUmbracoPostgresqlSupport(this IUmbracoBuilder builder)
    {
        // TryAddEnumerable takes both TService and TImplementation into consideration (unlike TryAddSingleton)
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ISqlSyntaxProvider, PostgresqlSyntaxProvider>());
        builder.Services.TryAddEnumerable(ServiceDescriptor
            .Singleton<IBulkSqlInsertProvider, PostgresqlBulkSqlInsertProvider>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IDatabaseCreator, PostgresqlDatabaseCreator>());
        builder.Services.TryAddEnumerable(ServiceDescriptor
            .Singleton<IProviderSpecificMapperFactory, PostgresqlSpecificMapperFactory>());
        builder.Services.TryAddEnumerable(ServiceDescriptor
            .Singleton<IDatabaseProviderMetadata, PostgresqlDatabaseProviderMetadata>());

        builder.Services.TryAddEnumerable(ServiceDescriptor
            .Singleton<IDistributedLockingMechanism, PostgresqlDistributedLockingMechanism>());

        builder.Services.TryAddEnumerable(ServiceDescriptor
            .Singleton<IProviderSpecificInterceptor, PostgresqlAddPreferDeferredInterceptor>());
        builder.Services.TryAddEnumerable(ServiceDescriptor
            .Singleton<IProviderSpecificInterceptor, PostgresqlAddMiniProfilerInterceptor>());
        builder.Services.TryAddEnumerable(ServiceDescriptor
            .Singleton<IProviderSpecificInterceptor, PostgresqlAddRetryPolicyInterceptor>());

        DbProviderFactories.UnregisterFactory(Constants.ProviderName);
        DbProviderFactories.RegisterFactory(Constants.ProviderName, Npgsql.NpgsqlFactory.Instance);

        // Remove this registration in Umbraco 12
        DbProviderFactories.UnregisterFactory(Constants.ProviderNameLegacy);
        DbProviderFactories.RegisterFactory(Constants.ProviderNameLegacy, Npgsql.NpgsqlFactory.Instance);

        // enable legacy timestamp behaviour (see https://www.npgsql.org/doc/release-notes/6.0.html#timestamp-rationalization-and-improvements
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        return builder;
    }
}
