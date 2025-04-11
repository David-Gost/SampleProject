using System.Data;

namespace Base.Interface.DB;

public interface IBaseDbConnection
{

    public IDbConnection OracleConnection(string dbConnectStr = "");

    public IDbConnection MySqlConnection(string dbConnectStr = "");

    // public IDbConnection SqlServerConnection(string dbConnectStr="");

    public IDbConnection PostgresqlConnection(string dbConnectStr = "");
}