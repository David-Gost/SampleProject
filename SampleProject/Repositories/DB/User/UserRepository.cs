using System.Linq.Expressions;
using Dapper;
using Dommel;
using Microsoft.VisualBasic.CompilerServices;
using Oracle.ManagedDataAccess.Client;
using SampleProject.Models.Custom.RequestFrom.User;
using SampleProject.Models.DB.User;
using SampleProject.Repositories.Base;

namespace SampleProject.Repositories.DB;

public class UserRepository : BaseDbRepository
{
    private readonly OracleConnection _connection;

    public UserRepository(IConfiguration configuration) : base(configuration)
    {
        _connection = (OracleConnection?)OracleConnection("");
    }

    /// <summary>
    /// 取得單筆會員資料
    /// </summary>
    /// <param name="inputData"></param>
    /// <returns></returns>
    public IDictionary<string, object> GetUserData(GetUserDataParam inputData)
    {
        var userId = inputData.userId ?? "";
        var account = inputData.account ?? "";
        var sql = BaseFilterUserSql(inputData);

        var dataResult = _connection
            .QueryFirstOrDefaultAsync(sql, new { filterUserId = userId, filterAccount = account }).Result;
        return ((dataResult == null ? new object() : dataResult) as IDictionary<string, object>)!;
    }

    /// <summary>
    /// 取得多筆User資料
    /// </summary>
    /// <param name="inputData"></param>
    /// <returns></returns>
    public IEnumerable<object> GetUserDatas(GetUserDataParam inputData)
    {
        var userId = inputData.userId ?? "";
        var account = inputData.account ?? "";
        var sql = BaseFilterUserSql(inputData);

        var resultData=_connection
            .QueryAsync(sql, new { filterUserId = userId, filterAccount = account }).Result;
        return resultData;
    }

    /// <summary>
    /// 基礎共用sql
    /// </summary>
    /// <param name="inputData"></param>
    /// <returns></returns>
    private string BaseFilterUserSql(GetUserDataParam inputData)
    {
        var userId = inputData.userId ?? "";
        var account = inputData.account ?? "";

        var sql = @"SELECT * FROM USERS ";
        if (!string.IsNullOrEmpty(userId))
        {
            sql += "WHERE USER_ID = :filterUserId ";
        }

        if (!string.IsNullOrEmpty(account))
        {
            sql += "AND ACCOUNT = :filterAccount ";
        }

        return sql;
    }
}