using System.Data.Common;
using Dapper;
using Dommel;
using MySqlConnector;
using SampleProject.Repositories.Base;
using SampleProject.Models.DB;

namespace SampleProject.Repositories.DB.Common;

public class CrontabTasksRepository : BaseDbRepository
{
    private readonly MySqlConnection _connection;

    public CrontabTasksRepository(IConfiguration configuration) : base(configuration)
    {
        _connection = (MySqlConnection?)MySqlConnection("ConnectionStrings:MariadbConnection");
    }

    public CrontabTasks? GetData()
    {
        return _connection.Select<CrontabTasks?>(p => p.id == 1).First();
    }
}