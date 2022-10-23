using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPoco;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseModelDefinitions;
using Umbraco.Cms.Infrastructure.Persistence.SqlSyntax;
using Umbraco.Extensions;
using ColumnInfo = Umbraco.Cms.Infrastructure.Persistence.SqlSyntax.ColumnInfo;

namespace Umbraco.Cms.Persistence.Postgresql.Services;

/// <summary>
/// Implements <see cref="ISqlSyntaxProvider"/> for PostgreSQL.
/// </summary>
public class PostgresqlSyntaxProvider : SqlSyntaxProviderBase<PostgresqlSyntaxProvider>
{
    private readonly IOptions<GlobalSettings> _globalSettings;
    private readonly ILogger<PostgresqlSyntaxProvider> _log;

    public PostgresqlSyntaxProvider(
        IOptions<GlobalSettings> globalSettings,
        ILogger<PostgresqlSyntaxProvider> log)
    {
        _globalSettings = globalSettings;
        _log = log;

        IntColumnDefinition = "INTEGER";
        LongColumnDefinition = "BIGINT";
        BoolColumnDefinition = "BOOL";

        GuidColumnDefinition = "UUID";
        DateTimeColumnDefinition = "TIMESTAMP WITHOUT TIME ZONE";
        DateTimeOffsetColumnDefinition = "TIMESTAMP WITH TIME ZONE";
        TimeColumnDefinition = "TIME";
        DecimalColumnDefinition = "NUMERIC";

        RealColumnDefinition = "REAL";

        BlobColumnDefinition = "BYTEA";
    }

    /// <inheritdoc />
    public override string ProviderName => Constants.ProviderName;

    /// <inheritdoc />
    public override string StringColumnDefinition => "TEXT";

    /// <inheritdoc />
    public override string StringLengthUnicodeColumnDefinitionFormat => "VARCHAR({0})";

    /// <inheritdoc />
    public override IsolationLevel DefaultIsolationLevel
        => IsolationLevel.ReadCommitted;

    /// <inheritdoc />
    public override string DbProvider => Constants.ProviderName;

    /// <inheritdoc />
    public override string ConvertIntegerToOrderableString => "reverse(substr(reverse('0000000000'||'{0}'), 1, 10))";

    /// <inheritdoc />
    public override string ConvertDecimalToOrderableString => "reverse(substr(reverse('0000000000'||'{0}'), 1, 10))";

    /// <inheritdoc />
    public override string ConvertDateToOrderableString => "{0}";

    /// <inheritdoc />
    public override bool SupportsIdentityInsert() => false;

    /// <inheritdoc />
    public override bool SupportsClustered() => false;

    /// <inheritdoc />
    public override string GetQuotedTableName(string? tableName) => $@"""{tableName!.ToLowerInvariant()}""";

    /// <inheritdoc />
    public override string GetQuotedColumnName(string? columnName) => $@"""{columnName!.ToLowerInvariant()}""";

    /// <inheritdoc/>
    public override DatabaseType GetUpdatedDatabaseType(DatabaseType current, string? connectionString)
        => new CustomPostgreSQLDatabaseType();

    /// <inheritdoc />
    public override string GetIndexType(IndexTypes indexTypes) =>
        indexTypes switch
        {
            IndexTypes.UniqueNonClustered => "UNIQUE",
            _ => string.Empty,
        };

    /// <inheritdoc />
    public override string Format(TableDefinition table)
    {
        var columns = Format(table.Columns);
        var primaryKey = FormatPrimaryKey(table);
        List<string> foreignKeys = Format(table.ForeignKeys);

        StringBuilder sb = new StringBuilder()
            .AppendLine($"CREATE TABLE {table.Name.ToLower()}")
            .AppendLine("(")
            .Append(columns);

        if (!string.IsNullOrEmpty(primaryKey))
        {
            _ = sb.AppendLine($", {primaryKey}");
        }

        foreach (var foreignKey in foreignKeys)
        {
            _ = sb.AppendLine($", {foreignKey}");
        }

        _ = sb.AppendLine(")");

        return sb.ToString();
    }

    /// <inheritdoc />
    public override List<string> Format(IEnumerable<ForeignKeyDefinition> foreignKeys)
        => foreignKeys.Select(Format).ToList();

    /// <inheritdoc />
    public override string Format(ForeignKeyDefinition foreignKey)
    {
        var constraintName = string.IsNullOrEmpty(foreignKey.Name)
            ? $"FK_{foreignKey.ForeignTable}_{foreignKey.PrimaryTable}_{foreignKey.PrimaryColumns.First()}"
            : foreignKey.Name;

        var localColumn = GetQuotedColumnName(foreignKey.ForeignColumns.First());
        var remoteColumn = GetQuotedColumnName(foreignKey.PrimaryColumns.First());
        var foreignTable = GetQuotedTableName(foreignKey.ForeignTable);
        var remoteTable = GetQuotedTableName(foreignKey.PrimaryTable);
        var onDelete = FormatCascade("DELETE", foreignKey.OnDelete);
        var onUpdate = FormatCascade("UPDATE", foreignKey.OnUpdate);

        return
            $"ALTER TABLE {foreignTable} ADD CONSTRAINT {constraintName} FOREIGN KEY ({localColumn}) REFERENCES {remoteTable} ({remoteColumn}) {onDelete} {onUpdate}";
    }

    /// <inheritdoc />
    public override IEnumerable<Tuple<string, string, string, bool>> GetDefinedIndexes(IDatabase db)
    {
        string sql = @"
SELECT
    tc.table_name AS tablename,
    tc.constraint_name AS indexname,
    kcu.column_name AS columnname,
    tc.constraint_type = 'UNIQUE' AS isunique
FROM
    information_schema.table_constraints tc
    JOIN information_schema.key_column_usage kcu ON
    tc.constraint_name = kcu.constraint_name
WHERE
    tc.table_schema = CURRENT_SCHEMA()";
        List<IndexMeta> items = db.Fetch<IndexMeta>(sql);

        return items
            .Select(item =>
                new Tuple<string, string, string, bool>(item.TableName, item.IndexName, item.ColumnName, item.IsUnique))
            .ToList();
    }

    /// <inheritdoc />
    public override string GetSpecialDbType(SpecialDbType dbType)
    {
        if (dbType == SpecialDbType.NCHAR)
        {
            return "CHAR";
        }

        if (dbType == SpecialDbType.NTEXT)
        {
            return "TEXT";
        }

        if (dbType == SpecialDbType.NVARCHARMAX)
        {
            return "TEXT";
        }

        return "TEXT";
    }

    /// <inheritdoc />
    public override string GetSpecialDbType(SpecialDbType dbType, int customSize)
    {
        if (dbType == SpecialDbType.NCHAR)
        {
            return $"CHAR({customSize})";
        }

        if (dbType == SpecialDbType.NTEXT)
        {
            return $"VARCHAR({customSize})";
        }

        if (dbType == SpecialDbType.NVARCHARMAX)
        {
            return "TEXT";
        }

        return "TEXT";
    }

    /// <inheritdoc />
    public override bool TryGetDefaultConstraint(IDatabase db, string? tableName, string columnName, out string constraintName)
    {
        // only column defaults in PostgreSQL it seems
        /*
        SELECT
            column_default
        FROM
            information_schema.columns;
        */
        constraintName = string.Empty;
        return false;
    }

    /// <inheritdoc />
    public override string FormatPrimaryKey(TableDefinition table)
    {
        ColumnDefinition? columnDefinition = table.Columns.FirstOrDefault(x => x.IsPrimaryKey);
        if (columnDefinition == null)
        {
            return string.Empty;
        }

        var constraintName = string.IsNullOrEmpty(columnDefinition.PrimaryKeyName)
            ? $"PK_{table.Name}"
            : columnDefinition.PrimaryKeyName;

        var columns = string.IsNullOrEmpty(columnDefinition.PrimaryKeyColumns)
            ? GetQuotedColumnName(columnDefinition.Name)
            : string.Join(", ", columnDefinition.PrimaryKeyColumns
                .Split(Core.Constants.CharArrays.CommaSpace, StringSplitOptions.RemoveEmptyEntries)
                .Select(GetQuotedColumnName));

        // We can't name the PK if it's set as a column constraint so add an alternate at table level.
        var constraintType = table.Columns.Any(x => x.IsIdentity)
            ? "UNIQUE"
            : "PRIMARY KEY";

        return $"CONSTRAINT {constraintName} {constraintType} ({columns})";
    }

    /// <inheritdoc />
    // PostgreSQL uses LIMIT as opposed to TOP
    // SELECT TOP 5 * FROM My_Table
    // SELECT * FROM My_Table LIMIT 5;
    public override Sql<ISqlContext> SelectTop(Sql<ISqlContext> sql, int top)
        => sql.Append($"LIMIT {top}");

    /// <inheritdoc />
    public override string Format(IEnumerable<ColumnDefinition> columns)
        => string.Join(',', columns.Select(Format));

    /// <inheritdoc />
    public override void HandleCreateTable(IDatabase database, TableDefinition tableDefinition, bool skipKeysAndIndexes = false)
    {
        var columns = Format(tableDefinition.Columns);
        var primaryKey = FormatPrimaryKey(tableDefinition);

        StringBuilder sb = new StringBuilder()
            .AppendLine($"CREATE TABLE {tableDefinition.Name}")
            .AppendLine("(")
            .Append(columns);

        if (!string.IsNullOrEmpty(primaryKey) && !skipKeysAndIndexes)
        {
            _ = sb.AppendLine($", {primaryKey}");
        }

        var createSql = sb.AppendLine(")").ToString();

        _log.LogInformation("Create table:\n {Sql}", createSql);
        _ = database.Execute(new Sql(createSql));

        if (skipKeysAndIndexes)
        {
            return;
        }

        List<string> indexSql = Format(tableDefinition.Indexes);
        foreach (var sql in indexSql)
        {
            _log.LogInformation("Create Index:\n {Sql}", sql);
            _ = database.Execute(new Sql(sql));
        }

        List<string> foreignKeys = Format(tableDefinition.ForeignKeys);
        foreach (var foreignKey in foreignKeys)
        {
            _log.LogInformation("Create Foreign Key:\n {Sql}", foreignKey);
            _ = database.Execute(new Sql(foreignKey));
        }
    }

    /// <inheritdoc />
    public override IEnumerable<string> GetTablesInSchema(IDatabase db) =>
        db.Fetch<string>(@"
SELECT
    t.table_name
FROM
    information_schema.tables t
WHERE
    t.table_type = 'BASE TABLE'
    AND t.table_schema = CURRENT_SCHEMA()");

    /// <inheritdoc />
    public override IEnumerable<ColumnInfo> GetColumnsInSchema(IDatabase db)
    {
        string sql = @"
SELECT
    c.table_name
    , c.ordinal_position
    , c.column_name
    , c.data_type
    , c.character_maximum_length
    , c.is_nullable = 'YES' AS is_nullable
FROM
    information_schema.columns c
WHERE
    c.table_schema = CURRENT_SCHEMA()
ORDER BY
    c.table_name
    , c.ordinal_position";
        List<ColumnInfoTable> columnInfos = db.Fetch<ColumnInfoTable>(sql);
        foreach (ColumnInfoTable columnInfo in columnInfos)
        {
            yield return new ColumnInfo(
                columnInfo.TableName,
                columnInfo.ColumnName,
                columnInfo.Ordinal,
                columnInfo.IsNullable,
                columnInfo.FormattedDataType);
        }
    }

    /// <inheritdoc />
    public override IEnumerable<Tuple<string, string, string>> GetConstraintsPerColumn(IDatabase db)
    {
        string sql = @"
SELECT
    ccu.table_name
    , ccu.column_name
    , ccu.constraint_name
FROM
    information_schema.constraint_column_usage ccu
WHERE
    ccu.table_schema = CURRENT_SCHEMA()";
        List<ConstraintPerColumn> items = db.Fetch<ConstraintPerColumn>(sql);

        // item.TableName, item.ColumnName, item.ConstraintName
        return items.Select(x => Tuple.Create(x.TableName, x.ColumnName, x.ConstraintName));
    }

    /// <inheritdoc />
    public override Sql<ISqlContext>.SqlJoinClause<ISqlContext> LeftJoinWithNestedJoin<TDto>(
        Sql<ISqlContext> sql,
        Func<Sql<ISqlContext>,
        Sql<ISqlContext>> nestedJoin,
        string? alias = null)
    {
        Type type = typeof(TDto);

        var tableName = GetQuotedTableName(type.GetTableName());
        var join = tableName;

        if (alias != null)
        {
            var quotedAlias = GetQuotedTableName(alias);
            join += " " + quotedAlias;
        }

        var nestedSql = new Sql<ISqlContext>(sql.SqlContext);
        nestedSql = nestedJoin(nestedSql);

        Sql<ISqlContext>.SqlJoinClause<ISqlContext> sqlJoin = sql.LeftJoin(join);
        sql.Append(nestedSql);
        return sqlJoin;
    }

    /// <inheritdoc />
    public override string GetFieldNameForUpdate<TDto>(Expression<Func<TDto, object?>> fieldSelector, string? tableAlias = null)
    {
        var field = (PropertyInfo)ExpressionHelper.FindProperty(fieldSelector).Item1;
        var fieldName = GetColumnName(field);
        return GetQuotedColumnName(fieldName);

        static string GetColumnName(PropertyInfo column)
        {
            ColumnAttribute? attr = column.FirstAttribute<ColumnAttribute>();
            return string.IsNullOrWhiteSpace(attr?.Name) ? column.Name : attr.Name;
        }
    }

    /// <inheritdoc />
    protected override string? FormatSystemMethods(SystemMethods systemMethod) =>
        systemMethod switch
        {
            SystemMethods.NewGuid => "gen_random_uuid()",
            SystemMethods.CurrentDateTime => "CURRENT_TIMESTAMP",
            _ => null,
        };

    /// <inheritdoc />
    protected override string FormatIdentity(ColumnDefinition column)
    {
        /* NOTE: We need AUTOINCREMENT, adds overhead but makes magic ids not break everything.
         * e.g. Cms.Core.Constants.Security.SuperUserId is -1
         * without the sqlite_sequence table we end up with the next user id = 0
         * but 0 is considered to not exist by our c# code and things explode */
        return column.IsIdentity ? "PRIMARY KEY GENERATED BY DEFAULT AS IDENTITY" : string.Empty;
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    private class ColumnInfoTable
    {
        [Column("table_name")]
        public string TableName { get; private set; } = null!;

        [Column("column_name")]
        public string ColumnName { get; private set; } = null!;

        [Column("ordinal_position")]
        public int Ordinal { get; private set; }

        [Column("is_nullable")]
        public bool IsNullable { get; private set; }

        [Column("data_type")]
        public string DataType { get; private set; } = null!;

        [Column("character_maximum_length")]
        public int? CharacterMaximumLength { get; set; }

        public string FormattedDataType => CharacterMaximumLength != null
            ? $"{DataType}({CharacterMaximumLength})"
            : DataType;

        private string DebuggerDisplay => $"{TableName}.{ColumnName} Ordinal={Ordinal} IsNullable={IsNullable} {FormattedDataType}";
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    private class Constraint
    {
        public Constraint(string tableName, string columnName, string constraintName)
        {
            TableName = tableName;
            ColumnName = columnName;
            ConstraintName = constraintName;
        }

        public string TableName { get; }

        public string ColumnName { get; }

        public string ConstraintName { get; }

        private string DebuggerDisplay => $"{TableName}.{ColumnName} {ConstraintName}";

        public override string ToString() => ConstraintName;
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    private class ConstraintPerColumn
    {
        [Column("table_name")]
        public string TableName { get; set; } = null!;

        [Column("column_name")]
        public string ColumnName { get; set; } = null!;

        [Column("constraint_name")]
        public string ConstraintName { get; set; } = null!;

        private string DebuggerDisplay => $"{TableName}.{ColumnName} {ConstraintName}";
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    private class IndexMeta
    {
        public string TableName { get; set; } = null!;

        public string IndexName { get; set; } = null!;

        public string ColumnName { get; set; } = null!;

        public bool IsUnique { get; set; }

        private string DebuggerDisplay => $"{TableName}.{ColumnName} {IndexName} Unique={IsUnique}";
    }
}
