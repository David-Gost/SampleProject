using Dommel;

namespace TestApi.Services.Base.DB.Extension;

/// <summary>
/// 擴充沒有支援Oracle
/// </summary>
public class OracleSqlBuilder : ISqlBuilder
{
    /// <inheritdoc/>
    public virtual string BuildInsert(Type type, string tableName, string[] columnNames, string[] paramNames) =>
        $"DECLARE\n    " +
        $"seq_column_name VARCHAR2(4000);" +
        $"id_returned NUMBER;" +
        $"sql_stmt  clob;" +
        $"BEGIN    " +
        $"    ---取得表序列欄位名稱\n    " +
        $"INSERT INTO {tableName} ({string.Join(", ", columnNames)})   VALUES ({string.Join(", ", paramNames)}) RETURNING USER_ID INTO id_returned; COMMIT;\n" +
        $" END;";

    /// <inheritdoc/>
    public virtual string BuildPaging(string? orderBy, int pageNumber, int pageSize)
    {
        var start = pageNumber >= 1 ? (pageNumber - 1) * pageSize : 0;
        return $" {orderBy} limit {start}, {pageSize}";
    }

    /// <inheritdoc/>
    public string PrefixParameter(string paramName) => $":{paramName}";

    /// <inheritdoc/>
    public string QuoteIdentifier(string identifier) => $"\"{identifier}\"";

    /// <inheritdoc/>
    public string LimitClause(int count) => $"limit {count}";

    /// <inheritdoc/>
    public string LikeExpression(string columnName, string parameterName) => $"{columnName} like {parameterName}";
}