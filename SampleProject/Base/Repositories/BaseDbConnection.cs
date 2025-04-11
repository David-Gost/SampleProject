using System.Data;
using System.Data.SqlClient;
using Base.Interface.DB;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;

namespace SampleProject.Base.Repositories;

public class BaseDbConnection : IBaseDbConnection 
{
    private readonly IConfiguration _configuration;
    
    public BaseDbConnection(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    /// <summary>
    /// Oracle 資料庫連線
    /// </summary>
    /// <param name="dbConnectStr"></param>
    /// <returns></returns>
    public IDbConnection OracleConnection(string dbConnectStr="")
    {
        if (dbConnectStr.Equals(""))
        {

            dbConnectStr = GetDefaultConnectionString();
        }
        return new OracleConnection(dbConnectStr);
    }

    /// <summary>
    /// Mysql 資料庫連線
    /// </summary>
    /// <param name="dbConnectStr"></param>
    /// <returns></returns>
    public IDbConnection MySqlConnection(string dbConnectStr="")
    {
        if (dbConnectStr.Equals(""))
        {

            dbConnectStr = GetDefaultConnectionString();
        }
        
        return new MySqlConnection(dbConnectStr);
    }

    // /// <summary>
    // /// Sql Server連線
    // /// </summary>
    // /// <param name="dbConnectStr"></param>
    // /// <returns></returns>
    // public IDbConnection SqlServerConnection(string dbConnectStr)
    // {
    //     
    //     if (dbConnectStr.Equals(""))
    //     {
    //
    //         dbConnectStr = GetDefaultConnectionString();
    //     }
    //
    //     return new SqlConnection(dbConnectStr);
    // }

    /// <summary>
    /// PostgreSQL 連線
    /// </summary>
    /// <param name="dbConnectStr"></param>
    /// <returns></returns>
    public IDbConnection PostgresqlConnection(string dbConnectStr = "")
    {
        if (dbConnectStr.Equals(""))
        {

            dbConnectStr = GetDefaultConnectionString();
        }

        return new NpgsqlConnection(dbConnectStr);
    }

    /// <summary>
    /// 取得預設連線字串
    /// </summary>
    /// <returns></returns>
    private string GetDefaultConnectionString()
    {
        var dbOption=_configuration.GetSection("DBConnection").GetSection("Default");
        return dbOption.GetValue<string>("ConnectionString","")!;
    }
}