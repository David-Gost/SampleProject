using SampleProject.Models.DB;
using SampleProject.Repositories.DB.Common;
using SampleProject.Services.Base;

namespace SampleProject.Services.DB.Common;

public class CrontabTasksService:BaseDbService
{

    private readonly CrontabTasksRepository _crontabTasksRepository;
    
    public CrontabTasksService(IConfiguration configuration) : base(configuration)
    {

        _crontabTasksRepository = new CrontabTasksRepository(configuration);
    }

    public CrontabTasks? GetData()
    {
       return _crontabTasksRepository.GetData();
    }
}