using System.Data;
using System.Data.SqlClient;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using SampleProject.Base.Interface.DB.Repositories;

namespace SampleProject.Base.Util;

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

            dbConnectStr = "ConnectionStrings:DefaultConnection";
        }
        return new OracleConnection(_configuration[dbConnectStr]);
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

            dbConnectStr = "ConnectionStrings:DefaultConnection";
        }
        
        return new MySqlConnection(_configuration[dbConnectStr]);
    }

    /// <summary>
    /// Sql Server連線
    /// </summary>
    /// <param name="dbConnectStr"></param>
    /// <returns></returns>
    public IDbConnection SqlServerConnection(string dbConnectStr)
    {
        
        if (dbConnectStr.Equals(""))
        {

            dbConnectStr = "ConnectionStrings:DefaultConnection";
        }

        return new SqlConnection(_configuration[dbConnectStr]);
    }

    /// <summary>
    /// PostgreSQL 連線
    /// </summary>
    /// <param name="dbConnectStr"></param>
    /// <returns></returns>
    public IDbConnection PostgresqlConnection(string dbConnectStr = "")
    {
        if (dbConnectStr.Equals(""))
        {

            dbConnectStr = "ConnectionStrings:DefaultConnection";
        }

        return new NpgsqlConnection(_configuration[dbConnectStr]);
    }
}