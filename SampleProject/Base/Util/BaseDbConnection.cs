using System.Data;
using System.Data.SqlClient;
using MySqlConnector;
using Oracle.ManagedDataAccess.Client;

namespace SampleProject.Base.Util;

public class BaseDbConnection 
{
    private readonly IConfiguration _configuration;
    
    protected BaseDbConnection(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    /// <summary>
    /// Oracle 資料庫連線
    /// </summary>
    /// <param name="dbConnectStr"></param>
    /// <returns></returns>
    protected IDbConnection OracleConnection(string dbConnectStr)
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
    protected IDbConnection MySqlConnection(string dbConnectStr)
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
    protected IDbConnection SqlServerConnection(string dbConnectStr)
    {
        
        if (dbConnectStr.Equals(""))
        {

            dbConnectStr = "ConnectionStrings:DefaultConnection";
        }

        return new SqlConnection(_configuration[dbConnectStr]);
    }
}