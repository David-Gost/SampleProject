using Dapper;
using Dommel;
using Dommel.Json;
using SampleProject.Base.Interface.DB;
using SampleProject.Base.Interface.DB.Repositories;
using SampleProject.Base.Repositories;
using SampleProject.Base.Util.DB.DommelJson;
using SampleProject.Models.DB.Common;

namespace SampleProject.Repositories.DB.Common;

public class TempMailRepository : BaseDbRepository
{
    public TempMailRepository(IConfiguration configuration, IBaseDbConnection baseDbConnection) : base(configuration, baseDbConnection)
    {
        SetDbConnection();
        DommelJsonMapper.AddJson(new DommelJsonOptions
        {
            EntityAssemblies = [typeof(TempMailModel).Assembly],
            JsonTypeHandler = () => new NpgJsonObjectTypeHandler(),
        });
    }

    /// <summary>
    /// 新增
    /// </summary>
    /// <param name="tempMailModel"></param>
    /// <returns></returns>
    public async Task<TempMailModel?>? InsertMailData(TempMailModel? tempMailModel)
    {
        if (tempMailModel == null)
        {
            return null;
        }

        var dataId = (int)await _dbConnection.InsertAsync(tempMailModel);

        if (dataId != 0)
        {
            return await Task.FromResult(dataId > 0 ? _dbConnection.GetAsync<TempMailModel>(dataId).Result! : null);
        }

        return null;
    }

    /// <summary>
    /// 更新
    /// </summary>
    /// <param name="tempMailModel"></param>
    /// <returns></returns>
    public async Task<TempMailModel?>? UpdateMailData(TempMailModel tempMailModel)
    {
        var dataId = tempMailModel.id;
        var updateResult = _dbConnection.UpdateAsync(tempMailModel).Result;

        if (dataId != 0 && updateResult)
        {
            return await Task.FromResult(dataId > 0 ? _dbConnection.GetAsync<TempMailModel>(dataId).Result! : null);
        }

        return null;
    }

    /// <summary>
    /// 取得多筆資料
    /// </summary>
    /// <param name="filterParams"></param>
    /// <param name="orderParams"></param>
    /// <param name="limit"></param>
    /// <param name="isOnce"></param>
    /// <returns></returns>
    public async Task<IEnumerable<TempMailModel>?> GetDatas(IDictionary<string, object>? filterParams = null,
        IDictionary<string, string>? orderParams = null,
        int limit = 0,
        bool isOnce = false)
    {
        return await BaseGetFilter(filterParams, orderParams, limit, isOnce);
    }

    /// <summary>
    /// 基礎篩選語法
    /// </summary>
    /// <param name="filterParams"></param>
    /// <param name="orderParams"></param>
    /// <param name="limit"></param>
    /// <param name="isOnce"></param>
    /// <returns></returns>
    private async Task<IEnumerable<TempMailModel>?> BaseGetFilter(IDictionary<string, object>? filterParams = null,
        IDictionary<string, string>? orderParams = null,
        int limit = 0, bool isOnce = false)
    {
        var query = "SELECT * FROM temp_mail";
        var whereParams = new List<IDictionary<string, string>>();

        List<int>? ids = null;
        List<int>? sendStatus = null;
        if (filterParams != null)
        {
            //篩選id資料
            if (filterParams.TryGetValue("filterIds", out var filterIds))
            {
                ids = (List<int>)filterIds;
                if (ids.Count != 0)
                {
                    whereParams.Add(CreateWhereParams("id = ANY (@filterIds) "));
                }
            }

            //篩選寄件狀態
            if (filterParams.TryGetValue("filterSendStatus", out var filterSendStatus))
            {
                sendStatus = (List<int>)filterSendStatus;
                if (sendStatus.Count > 0)
                {
                    whereParams.Add(CreateWhereParams("send_status = ANY (@filterSendStatus)"));
                }
            }
        }

        //排序
        query += FilterParamsToQuery(whereParams) + OrderByParamsToQuery(orderParams!);

        if (limit > 0)
        {
            query += $" LIMIT {limit}";
        }

        var result = await _dbConnection.QueryAsync<TempMailModel>(query, new
        {
            filterIds = ids,
            filterSendStatus = sendStatus,
        });

        if (isOnce)
        {
            DisposeConnect();
        }

        return result;
    }
}