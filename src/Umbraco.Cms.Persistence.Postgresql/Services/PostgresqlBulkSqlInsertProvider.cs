using NPoco;
using Umbraco.Cms.Infrastructure.Persistence;

namespace Umbraco.Cms.Persistence.Postgresql.Services;

/// <summary>
///     Implements <see cref="IBulkSqlInsertProvider" /> for PostgreSQL.
/// </summary>
public class PostgresqlBulkSqlInsertProvider : IBulkSqlInsertProvider
{
    public string ProviderName => Constants.ProviderName;

    public int BulkInsertRecords<T>(IUmbracoDatabase database, IEnumerable<T> records)
    {
        T[] recordsA = records.ToArray();
        if (recordsA.Length == 0)
        {
            return 0;
        }

        PocoData? pocoData = database.PocoDataFactory.ForType(typeof(T));
        if (pocoData == null)
        {
            throw new InvalidOperationException("Could not find PocoData for " + typeof(T));
        }

        return BulkInsertRecordsPostgresql(database, pocoData, recordsA);
    }

    /// <summary>
    ///     Bulk-insert records using PostgreSQL COPY method.
    /// </summary>
    /// <typeparam name="T">The type of the records.</typeparam>
    /// <param name="database">The database.</param>
    /// <param name="pocoData">The PocoData object corresponding to the record's type.</param>
    /// <param name="records">The records.</param>
    /// <returns>The number of records that were inserted.</returns>
    private int BulkInsertRecordsPostgresql<T>(IUmbracoDatabase database, PocoData pocoData, IEnumerable<T> records)
    {
        var count = 0;
        var inTrans = database.InTransaction;

        if (!inTrans)
        {
            database.BeginTransaction();
        }

        foreach (T record in records)
        {
            database.Insert(record);
            count++;
        }

        if (!inTrans)
        {
            database.CompleteTransaction();
        }

        return count;
    }
}
