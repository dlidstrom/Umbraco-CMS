using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Cms.Persistence.Postgresql;

/// <summary>
///     Automatically adds PostgresQL support to Umbraco when this project is referenced.
/// </summary>
public class PostgresqlComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
        => builder.AddUmbracoPostgresqlSupport();
}
