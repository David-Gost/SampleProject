using Oracle.ManagedDataAccess.Client;
using SampleProject.Until.Base;

namespace SampleProject.Repositories.Base;

/// <summary>
/// DbRepository核心
/// </summary>
public class BaseDbRepository : BaseDbConnection
{
    protected BaseDbRepository(IConfiguration configuration) : base(configuration)
    {
    }
}