using System.Data;
using SampleProject.Base.Repositories;
using Dapper;
using Base.Util.DB.Dapper;
using SampleProject.Base.Util.DB.EFCore;
using SampleProject.Database;

namespace SampleProject.Repositories.DB.Common;

public class CrontabTasksRepository:BaseDbRepository
{
    public CrontabTasksRepository(DapperContextManager dapperContextManager, DbContextManager dbContextManager, IDbConnection dapperDbConnection, ApplicationDbContext efDbConnection) : base(dapperContextManager, dbContextManager, dapperDbConnection, efDbConnection)
    {
        SetDbConnection("LOCAL_MARIADB");
    }

    public IEnumerable<dynamic> GetAllData()
    {
        return _dapperDbConnection.QueryAsync("SELECT * FROM crontab_tasks").Result;
    }
}