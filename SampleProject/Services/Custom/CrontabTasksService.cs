using SampleProject.Repositories.DB.Common;

namespace SampleProject.Services.Custom;

public class CrontabTasksService
{

    private readonly CrontabTasksRepository _crontabTasksRepository;
    public CrontabTasksService(CrontabTasksRepository crontabTasksRepository)
    {

        _crontabTasksRepository = crontabTasksRepository;
    }

    public IEnumerable<dynamic> GetAllData()
    {
        
       return _crontabTasksRepository.GetAllData();
    }
}