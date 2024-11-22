using System.Data;
using SampleProject.Base.Interface.DB;
using SampleProject.Base.Interface.DB.Repositories;

namespace SampleProject.Base.Util.DB.Dapper;

public class DapperContextManager
{
    private readonly IConfiguration _configuration;
    private readonly IBaseDbConnection _baseDbConnection;

    public DapperContextManager(IConfiguration configuration, IBaseDbConnection baseDbConnection)
    {
        _configuration = configuration;
        _baseDbConnection = baseDbConnection;
    }

    /// <summary>
    /// 使用Dapper依照設定檔建立連線
    /// </summary>
    /// <param name="dbConnectOption"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public IDbConnection CreateDbConnection(string dbConnectOption = "Default")
    {
        var dbOption = _configuration.GetSection("DBConnection").GetSection(dbConnectOption);
        var dbConnectType = dbOption.GetValue<string>("DBType", "")!.ToUpper();
        var dbConnectStr = dbOption.GetValue<string>("ConnectionString", "")!;

        var dbType = dbConnectType switch
        {
            "ORACLE" => DBType.ORACLE,
            "MYSQL" => DBType.MYSQL,
            "MSSQL" => DBType.MSSQL,
            "POSTGRESQL" => DBType.POSTGRESQL,
            _ => throw new ArgumentException("Invalid database type"),
        };

        return dbType switch
        {
            DBType.ORACLE => _baseDbConnection.OracleConnection(dbConnectStr),
            DBType.MYSQL => _baseDbConnection.MySqlConnection(dbConnectStr),
            DBType.MSSQL => _baseDbConnection.SqlServerConnection(dbConnectStr),
            DBType.POSTGRESQL => _baseDbConnection.PostgresqlConnection(dbConnectStr),
            _ => throw new ArgumentException("Invalid database type"),
        };
    }
}