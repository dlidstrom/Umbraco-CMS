namespace Umbraco.Cms.Persistence.Postgresql;

/// <summary>
///     Constants related to PostgreSQL.
/// </summary>
public static class Constants
{
    /// <summary>
    ///     PostgreSQL provider name.
    /// </summary>
    public const string ProviderName = "Npgsql";

    [Obsolete("This will be removed in Umbraco 12. Use Constants.ProviderName instead")]
    public const string ProviderNameLegacy = "Npgsql";
}
