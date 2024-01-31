namespace SampleProject.Services.Base;

/// <summary>
/// DbService核心
/// </summary>
public class BaseDbService
{
    protected readonly IConfiguration _configuration;

    protected BaseDbService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
}