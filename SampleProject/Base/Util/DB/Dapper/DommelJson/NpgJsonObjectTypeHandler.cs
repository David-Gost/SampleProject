using System.Data;
using Dapper;
using Npgsql;

namespace SampleProject.Base.Util.DB.Dapper.DommelJson;

/// <summary>
/// postgresqlJson欄位處理
/// </summary>
public class NpgJsonObjectTypeHandler : SqlMapper.ITypeHandler
{
 
    private readonly JsonObjectTypeHandler _defaultTypeHandler = new();

    public void SetValue(IDbDataParameter parameter, object value)
    {
        // Use the default handler
        _defaultTypeHandler.SetValue(parameter, value);

        // Set the special NpgsqlDbType to use the JSON data type
        if (parameter is NpgsqlParameter npgsqlParameter)
        {
            npgsqlParameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Json;
        }
    }

    public object? Parse(Type destinationType, object value) =>
        _defaultTypeHandler.Parse(destinationType, value);
}