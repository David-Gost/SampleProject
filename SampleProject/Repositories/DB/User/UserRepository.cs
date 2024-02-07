using System.Dynamic;
using System.Linq.Expressions;
using Dapper;
using Dommel;
using Microsoft.VisualBasic.CompilerServices;
using Oracle.ManagedDataAccess.Client;
using SampleProject.Base.Repositories;
using SampleProject.Models.Custom.RequestFrom.User;
using SampleProject.Models.DB.User;


namespace SampleProject.Repositories.DB.User;

public class UserRepository : BaseDbRepository
{
    public UserRepository(IConfiguration configuration) : base(configuration)
    {
        _dbConnection = (OracleConnection?)OracleConnection("");
    }

    /// <summary>
    /// 取得單筆會員資料
    /// </summary>
    /// <param name="inputData"></param>
    /// <returns></returns>
    public Users? GetUserData(GetUserDataParam inputData)
    {
        var userId = inputData.userId;
        var account = inputData.account ?? "";
        // var sql = BaseFilterUserSql(inputData);

        using (_dbConnection)
        {
            var dataResult = _dbConnection.GetAllAsync<Users, UserInfos, Users>(
                    (user, userInfo) =>
                    {
                        if (userId > 0)
                        {
                            user.userId = userId;
                        }

                        user.UserInfo = userInfo;
                        return user;
                    }
                )
                .Result.FirstOrDefault();

            return dataResult;
        }
    }

    /// <summary>
    /// 取得多筆User資料
    /// </summary>
    /// <param name="inputData"></param>
    /// <returns></returns>
    public IEnumerable<object> GetUserDatas(GetUserDataParam inputData)
    {
        var userId = inputData.userId;
        var account = inputData.account ?? "";
        var sql = BaseFilterUserSql(inputData);

        var resultData = _dbConnection
            .QueryAsync(sql, new { filterUserId = userId, filterAccount = account }).Result;
        return resultData;
    }

    /// <summary>
    /// 新增user資料
    /// </summary>
    /// <param name="userData"></param>
    /// <returns></returns>
    public Users? AddUserData(Users userData)
    {
        var insertId = InsertIntoOracle(userData);

        return insertId != 0 ?  _dbConnection.GetAsync<Users>(insertId).Result : null;
    }

    /// <summary>
    /// 基礎共用sql
    /// </summary>
    /// <param name="inputData"></param>
    /// <returns></returns>
    private string BaseFilterUserSql(GetUserDataParam inputData)
    {
        var userId = inputData.userId;
        var account = inputData.account ?? "";

        var sql = @"SELECT USERS.*,USER_INFOS.TELEPHONE FROM USERS 
    LEFT JOIN USER_INFOS ON USERS.USER_ID = USER_INFOS.USER_ID ";
        if (userId > 0)
        {
            sql += "WHERE USERS.USER_ID = :filterUserId ";
        }

        if (!string.IsNullOrEmpty(account))
        {
            sql += "AND ACCOUNT = :filterAccount ";
        }

        return sql;
    }
}