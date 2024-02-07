using System.Runtime.CompilerServices;
using Dommel;

namespace SampleProject.Base.Util.DB.DommelBuilder;

/// <summary>
/// 擴充Dapper.Dommel沒有支援Oracle
/// </summary>
public class OracleSqlBuilder : ISqlBuilder
{
    /// <inheritdoc/>
    public virtual string BuildInsert(Type type, string tableName, string[] columnNames, string[] paramNames){
        
        var columns = string.Join(", ", columnNames);
        var values = string.Join(", ", paramNames);
        
        return $"INSERT INTO {tableName} ({columns}) VALUES ({values})";
    }

    /// <inheritdoc/>
    public virtual string BuildPaging(string? orderBy, int pageNumber, int pageSize)
    {
        var start = pageNumber >= 1 ? (pageNumber - 1) * pageSize : 0;
        return $" {orderBy} offset {start} rows fetch next {pageSize} only";
    }

    /// <inheritdoc/>
    public string PrefixParameter(string paramName) => $":{paramName}";

    /// <inheritdoc/>
    public string QuoteIdentifier(string identifier) => $"\"{identifier}\"";

    /// <inheritdoc/>
    public string LimitClause(int count) => $"fetch first {count} rows only ";

    /// <inheritdoc/>
    public string LikeExpression(string columnName, string parameterName) => $"{columnName} like {parameterName}";
}