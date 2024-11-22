using System.Data;
using Dapper;
using SampleProject.Base.Interface.DB;
using SampleProject.Base.Interface.DB.Repositories;
using SampleProject.Base.Repositories;
using SampleProject.Base.Util.DB.Dapper;
using SampleProject.Base.Util.DB.EFCore;
using SampleProject.Util;

namespace SampleProject.Repositories.DB.Common;

public class CrontabTasksRepository:BaseDbRepository
{
    public CrontabTasksRepository(DapperContextManager dapperContextManager, DbContextManager dbContextManager, IDbConnection dapperDbConnection, ApplicationDbContext efDbConnection) : base(dapperContextManager, dbContextManager, dapperDbConnection, efDbConnection)
    {
    }

    public IEnumerable<dynamic> GetAllData()
    {
        return _dapperDbConnection.QueryAsync("SELECT * FROM crontab_tasks").Result;
    }
}