using System.Data;
using Dapper;
using Dommel;
using Dommel.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SampleProject.Base.Interface.DB;
using SampleProject.Base.Interface.DB.Repositories;
using SampleProject.Base.Repositories;
using SampleProject.Base.Util.DB.Dapper;
using SampleProject.Base.Util.DB.Dapper.DommelJson;
using SampleProject.Base.Util.DB.EFCore;
using SampleProject.Models.DB.Common;
using SampleProject.Database;

namespace SampleProject.Repositories.DB.Common;

public class TempMailRepository : BaseDbRepository
{
    public TempMailRepository(DapperContextManager dapperContextManager, DbContextManager dbContextManager,
        IDbConnection dapperDbConnection, ApplicationDbContext efDbConnection) : base(dapperContextManager,
        dbContextManager, dapperDbConnection, efDbConnection)
    {
        SetDbConnection("CLOUD_POSTGRESQL");
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

        await using (_efDbConnection)
        {
            var result = await _efDbConnection.AddAsync(tempMailModel);
            await _efDbConnection.SaveChangesAsync();
            Console.WriteLine(_efDbConnection.TempMails.ToQueryString());
            return await Task.FromResult(result.Entity);
        }
    }

    /// <summary>
    /// 更新
    /// </summary>
    /// <param name="tempMailModel"></param>
    /// <returns></returns>
    public async Task<TempMailModel?>? UpdateMailData(TempMailModel tempMailModel)
    {
        var dataId = tempMailModel.id;
        var updateResult = _dapperDbConnection.UpdateAsync(tempMailModel).Result;

        if (dataId != 0 && updateResult)
        {
            return await Task.FromResult(
                dataId > 0 ? _dapperDbConnection.GetAsync<TempMailModel>(dataId).Result! : null);
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
        var dbSet = BaseGetFilter(filterParams, orderParams);
        var result = await dbSet.ToListAsync();

        if (isOnce)
        {
            DisposeConnect();
        }

        return result;
    }

    /// <summary>
    /// 基礎篩選語法
    /// </summary>
    /// <param name="filterParams"></param>
    /// <param name="orderParams"></param>
    /// <returns></returns>
    private IQueryable<TempMailModel> BaseGetFilter(IDictionary<string, object>? filterParams = null,
        IDictionary<string, string>? orderParams = null)
    {
        var dbSet = _efDbConnection.TempMails.AsQueryable();

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
                    dbSet = dbSet.Where(x => ids.Contains(x.id));
                }
            }

            //篩選寄件狀態
            if (filterParams.TryGetValue("filterSendStatus", out var filterSendStatus))
            {
                sendStatus = (List<int>)filterSendStatus;
                if (sendStatus.Count > 0)
                {
                    dbSet = dbSet.Where(x => sendStatus.Contains(x.sendStatus));
                }
            }
        }

        //排序
        DbSetOrderBy(ref dbSet, orderParams!);
        return dbSet;
    }
}