using System.Collections;
using System.Data.Common;
using System.Reflection;
using NPoco;

namespace Umbraco.Cms.Persistence.Postgresql.Mappers;

// not needed it seems
public class PostgresqlBitMapper : DefaultMapper
{
    private static readonly BitArray _true = new(1, true);
    private static readonly BitArray _false = new(1, false);

    public override Func<object, object> GetToDbConverter(Type destType, MemberInfo sourceMemberInfo)
    {
        if (destType == typeof(bool))
        {
            return val =>
            {
                return Convert.ToBoolean(val) ? _true : _false;
            };
        }

        return base.GetToDbConverter(destType, sourceMemberInfo);
    }

    public override Func<object, object> GetParameterConverter(DbCommand dbCommand, Type sourceType)
    {
        if (sourceType == typeof(bool))
        {
            return val =>
            {
                return Convert.ToBoolean(val) ? _true : _false;
            };
        }

        return base.GetParameterConverter(dbCommand, sourceType);
    }
}
