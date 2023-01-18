using System.Diagnostics;
using System.Runtime.Serialization;
using Npgsql;
using Umbraco.Cms.Core.Install.Models;
using Umbraco.Cms.Infrastructure.Persistence;

namespace Umbraco.Cms.Persistence.Postgresql.Services;

[DataContract]
public class PostgresqlDatabaseProviderMetadata : IDatabaseProviderMetadata
{
    /// <inheritdoc />
    public Guid Id => new("3A20EBA4-EFA9-4128-927A-07742112DFD1");

    /// <inheritdoc />
    public int SortOrder => -1;

    /// <inheritdoc />
    public string DisplayName => "PostgreSQL (experimental)";

    /// <inheritdoc />
    public string DefaultDatabaseName => Core.Constants.System.UmbracoDefaultDatabaseName;

    /// <inheritdoc />
    public string ProviderName => Constants.ProviderName;

    /// <inheritdoc />
    public bool SupportsQuickInstall => false;

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <inheritdoc />
    public bool RequiresServer => true;

    /// <inheritdoc />
    public string? ServerPlaceholder => null;

    /// <inheritdoc />
    public bool RequiresCredentials => true;

    /// <inheritdoc />
    public bool SupportsIntegratedAuthentication => false;

    /// <inheritdoc />
    public bool RequiresConnectionTest => true;

    /// <inheritdoc />
    public bool ForceCreateDatabase => false;

    /// <inheritdoc />
    public string GenerateConnectionString(DatabaseModel databaseModel)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = databaseModel.Server,
            Database = databaseModel.DatabaseName,
            Username = databaseModel.Login,
            Password = databaseModel.Password,
            Pooling = true,
            IncludeErrorDetail = Debugger.IsAttached,
        };

        return builder.ConnectionString;
    }
}
