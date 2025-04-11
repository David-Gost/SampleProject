using System.Data;

using Dapper;
using Dommel;
using SampleProject.Base.Repositories;
using Base.Util.DB.Dapper;
using SampleProject.Base.Util.DB.EFCore;
using SampleProject.Models.Custom.RequestFrom.User;
using SampleProject.Models.DB.User;
using SampleProject.Database;


namespace SampleProject.Repositories.DB.User;

public class UserRepository : BaseDbRepository
{
    public UserRepository(DapperContextManager dapperContextManager, DbContextManager dbContextManager, IDbConnection dapperDbConnection, ApplicationDbContext efDbConnection) : base(dapperContextManager, dbContextManager, dapperDbConnection, efDbConnection)
    {
        SetDbConnection("CLOUD_POSTGRESQL");
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

        using (_dapperDbConnection)
        {
            var dataResult = _dapperDbConnection.GetAllAsync<Users, UserInfos, Users>(
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

        var resultData = _dapperDbConnection
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

        return insertId != 0 ?  _dapperDbConnection.GetAsync<Users>(insertId).Result : null;
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