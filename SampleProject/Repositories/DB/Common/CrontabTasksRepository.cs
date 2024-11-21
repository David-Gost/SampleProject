using Dapper;
using SampleProject.Base.Interface.DB;
using SampleProject.Base.Interface.DB.Repositories;
using SampleProject.Base.Repositories;

namespace SampleProject.Repositories.DB.Common;

public class CrontabTasksRepository:BaseDbRepository
{
    public CrontabTasksRepository(IConfiguration configuration, IBaseDbConnection baseDbConnection) : base(configuration, baseDbConnection)
    {
        
        SetDbConnection();
    }


    public IEnumerable<dynamic> GetAllData()
    {
        return _dbConnection.QueryAsync("SELECT * FROM crontab_tasks").Result;
    }
}