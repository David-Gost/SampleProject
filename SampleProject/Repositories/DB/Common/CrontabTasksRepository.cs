using Dapper;
using SampleProject.Base.Interface.DB.Repositories;
using SampleProject.Base.Repositories;

namespace SampleProject.Repositories.DB.Common;

public class CrontabTasksRepository:BaseDbRepository
{
    public CrontabTasksRepository(IBaseDbConnection baseDbConnection) : base(baseDbConnection)
    {
        
        SetDbConnection(DBType.MYSQL,"ConnectionStrings:MariadbConnection");
    }


    public IEnumerable<dynamic> GetAllData()
    {
        return _dbConnection.QueryAsync("SELECT * FROM crontab_tasks").Result;
    }
}