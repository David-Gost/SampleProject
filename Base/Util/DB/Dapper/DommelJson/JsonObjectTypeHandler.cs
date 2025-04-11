using System.Data;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Base.Util.DB.Dapper.DommelJson;

/// <summary>
/// 資料庫欄位型別Json處理（只處理JsonObject）
/// </summary>
public class JsonObjectTypeHandler : SqlMapper.ITypeHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public void SetValue(IDbDataParameter parameter, object? value)
    {
        parameter.Value = value is null or DBNull
            ? DBNull.Value
            : JsonConvert.SerializeObject(value);
        parameter.DbType = DbType.String;
    }

    public dynamic? Parse(Type destinationType, object? value)
    {
        return value is string str ? JObject.Parse(str).ToObject<ExpandoObject>() : null;
    }
}