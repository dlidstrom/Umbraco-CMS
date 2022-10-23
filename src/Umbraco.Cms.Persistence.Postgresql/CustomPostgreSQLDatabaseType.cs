using NPoco.DatabaseTypes;

namespace Umbraco.Cms.Persistence.Postgresql;

/// <summary>
/// Customized database type that does no quoting.
/// </summary>
public class CustomPostgreSQLDatabaseType : PostgreSQLDatabaseType
{
    /// <summary>
    /// Quote identifiers in lowercase. This allows reserved words to be used.
    /// </summary>
    public override string EscapeSqlIdentifier(string str) => $@"""{str.ToLowerInvariant()}""";

    /// <inheritdoc/>
    public override string GetParameterPrefix(string connectionString) => "@";
}
