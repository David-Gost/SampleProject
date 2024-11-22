using System.Data;
using Dapper;
using Dapper.Oracle;
using Dommel;
using Microsoft.EntityFrameworkCore;
using SampleProject.Base.Interface.DB;
using SampleProject.Base.Interface.DB.Repositories;
using SampleProject.Base.Models.DB;
using SampleProject.Base.Util.DB.Dapper;
using SampleProject.Base.Util.DB.EFCore;
using SampleProject.Helpers;
using SampleProject.Util;

namespace SampleProject.Base.Repositories;

/// <summary>
/// DbRepository核心
/// </summary>
public class BaseDbRepository
{
    /// <summary>
    /// dapper 連接器
    /// </summary>
    protected IDbConnection _dapperDbConnection { set; get; }

    protected ApplicationDbContext _efDbConnection { set; get; }

    private readonly DapperContextManager _dapperContextManager;
    private readonly DbContextManager _dbContextManager;

    public BaseDbRepository(DapperContextManager dapperContextManager,
        DbContextManager dbContextManager,
        IDbConnection dapperDbConnection,
        ApplicationDbContext efDbConnection)
    {
        _dapperContextManager = dapperContextManager;
        _dbContextManager = dbContextManager;
        _dapperDbConnection = dapperDbConnection;
        _efDbConnection = efDbConnection;
    }

    /// <summary>
    /// 設定DB連線，用於
    /// </summary>
    /// <param name="dbConnectOption">db連線資料，預設帶入Default</param>
    /// <exception cref="ArgumentException"></exception>
    protected void SetDbConnection(
        string dbConnectOption = "Default")
    {
        if (string.IsNullOrEmpty(dbConnectOption))
        {
            throw new ArgumentException("Invalid database connection option");
        }

        _dapperDbConnection = _dapperContextManager.CreateDbConnection(dbConnectOption);
        _efDbConnection = _dbContextManager.CreateDbContext(dbConnectOption);
    }

    /// <summary>
    /// 檢查db連線是否啟用
    /// </summary>
    /// <returns></returns>
    protected bool CheckConnectOpen()
    {
        try
        {
            if (_dapperDbConnection.State == ConnectionState.Closed)
            {
                _dapperDbConnection.Open();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }

        return _dapperDbConnection.State == ConnectionState.Open;
    }

    /// <summary>
    /// 關閉db連線
    /// </summary>
    protected void DisposeConnect()
    {
        try
        {
            _dapperDbConnection.Dispose();
            _efDbConnection.Dispose();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    /// <summary>
    /// 產生篩選條件資料
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="parameterVal"></param>
    /// <param name="separator"></param>
    /// <returns></returns>
    protected IDictionary<string, string> CreateWhereParams(string condition, string parameterVal = "",
        string separator = "AND")
    {
        var sqlBuilder = GetSqlBuilder();

        return new Dictionary<string, string>
        {
            { "condition", condition },
            { "parameterVal", !parameterVal.Equals("") ? sqlBuilder.PrefixParameter(parameterVal) : "" },
            { "separator", separator }
        };
    }

    /// <summary>
    /// 條件語法轉換
    /// </summary>
    /// <param name="filterList"></param>
    /// <param name="generateWhere">是否從where字串開始產生</param>
    /// <returns></returns>
    protected string FilterParamsToQuery(List<IDictionary<string, string>>? filterList, bool generateWhere = true)
    {
        var filterQuery = "";

        if (filterList is not { Count: > 0 })
        {
            return filterQuery;
        }

        foreach (var dictionary in filterList)
        {
            var condition = dictionary["condition"];
            var parameterVal = dictionary["parameterVal"];
            var separator = dictionary["separator"];

            if (filterQuery.Equals("") && generateWhere)
            {
                filterQuery = $" WHERE {condition} {parameterVal} ";
            }
            else
            {
                filterQuery += $" {separator} {condition} {parameterVal}";
            }
        }

        return filterQuery;
    }

    /// <summary>
    /// 產生group by 語法
    /// </summary>
    /// <param name="groupByList"></param>
    /// <returns></returns>
    protected string GroupByParamsToQuery(List<string> groupByList)
    {
        if (groupByList is not { Count: > 0 })
        {
            return "";
        }

        return " GROUP BY " + string.Join(" ,", groupByList);
    }

    /// <summary>
    /// 產生order sql 語法
    /// </summary>
    /// <param name="orderParams"></param>
    /// <returns></returns>
    protected string OrderByParamsToQuery(IDictionary<string, string> orderParams)
    {
        if (orderParams is not { Count: > 0 })
        {
            return "";
        }

        var orderByList = new List<string?>();
        for (var i = 0; i < orderParams.Keys.Count; i++)
        {
            var propertyName = orderParams.Keys.ElementAt(i);
            var orderVal = orderParams[propertyName];
            orderByList.Add($"{propertyName} {GetOrderVal(orderVal)}");
        }

        return $" ORDER BY {string.Join(" ,", orderByList)}";
    }

    /// <summary>
    /// ef core用order by 語法
    /// </summary>
    /// <param name="dbSet"></param>
    /// <param name="orderParams"></param>
    /// <typeparam name="TEntity"></typeparam>
    protected void DbSetOrderBy<TEntity>(ref IQueryable<TEntity> dbSet, IDictionary<string, string> orderParams)
        where TEntity : class
    {
        if (orderParams is not { Count: > 0 })
        {
            return;
        }

        var isFirstOrder = true;
        IOrderedQueryable<TEntity>? orderedDbSet = null;

        foreach (var orderParam in orderParams)
        {
            var propertyName = orderParam.Key;
            var orderVal = orderParam.Value;
            var isAscending = GetOrderVal(orderVal).Equals("ASC");

            if (isFirstOrder)
            {
                orderedDbSet = isAscending
                    ? dbSet.OrderBy(x => EF.Property<object>(x, propertyName))
                    : dbSet.OrderByDescending(x => EF.Property<object>(x, propertyName));
                isFirstOrder = false;
            }
            else
            {
                orderedDbSet = isAscending
                    ? orderedDbSet!.ThenBy(x => EF.Property<object>(x, propertyName))
                    : orderedDbSet!.ThenByDescending(x => EF.Property<object>(x, propertyName));
            }
        }

        dbSet = orderedDbSet ?? dbSet;
    }

    /// <summary>
    /// ef core用產生分頁資料
    /// </summary>
    /// <param name="dbSet"></param>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    protected async Task<PaginationModel<T>> DbSetPagination<T>(
        IQueryable<T> dbSet,
        int pageNumber = 1,
        int pageSize = 15)
    {
        // 計算總記錄數
        var dataCount = await dbSet.CountAsync();
        
        //計算總頁數
        var totalPages = (int)Math.Ceiling((double)dataCount / pageSize);
        
        // 檢查頁碼是否超出範圍
        if (pageNumber < 1)
        {
            pageNumber = 1;
        }
        else if (pageNumber > totalPages)
        {
            pageNumber = totalPages;
        }

        // 應用分頁
        var pagedData = await dbSet
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginationModel<T>
        {
            dataCount = dataCount,
            totalPages = totalPages,
            pageNumber = pageNumber,
            pageSize = pageSize,
            data = pagedData
        };
    }

    /// <summary>
    /// 紀錄欄位時間
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="columnList"></param>
    /// <typeparam name="TEntity"></typeparam>
    protected void UpdateColumnTimestamp<TEntity>(ref TEntity? entity, List<string> columnList) where TEntity : class
    {
        if (entity == null || columnList.Count <= 0) return;
        var dataType = typeof(TEntity);
        //轉換為字典，並依照欄位值更新時間
        var paramDictionary = SystemHelper.EntityToDictionary(entity);
        var nowTime = DateTime.UtcNow;
        foreach (var columnName in columnList.Where(columnName => paramDictionary.ContainsKey(columnName)))
        {
            paramDictionary[columnName] = nowTime;
        }

        entity = SystemHelper.ConvertDictionaryToEntity(dataType, paramDictionary!) as TEntity;
    }

    /// <summary>
    /// 使用Dapper的擴充套件Dommel撰寫的oracle專用新增資料語法
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns>新增成功回傳1，失敗回應0，有設定自動序列的鍵值時會回傳新增後的序列值</returns>
    protected int InsertIntoOracle<TEntity>(TEntity entity)
    {
        if (!CheckConnectOpen())
        {
            return 0;
        }

        var returnData = 0;
        var dataType = typeof(TEntity);
        var sqlBuilder = GetSqlBuilder();

        //表名稱
        var tableName = Resolvers.Table(dataType, sqlBuilder);

        //產生model中所有值欄位資料
        var keyProperties = Resolvers.KeyProperties(dataType);

        //表鍵值
        var properties = keyProperties.Where(p => p.IsGenerated).Select(p => p.Property);

        //取第一鍵值
        var property = properties.FirstOrDefault();
        var typeProperties = Resolvers.Properties(dataType)
            .Where(x => !x.IsGenerated)
            .Select(x => x.Property)
            .Except(keyProperties.Where(p => p.IsGenerated).Select(p => p.Property)).ToList();

        //表欄位
        var columnNames = typeProperties.Select(p => Resolvers.Column(p, sqlBuilder, false)).ToArray();

        //數值欄位
        var paramNames = typeProperties.Select(p => sqlBuilder.PrefixParameter(p.Name)).ToArray();

        //產生insert 語句
        var insertSql = sqlBuilder.BuildInsert(dataType, tableName, columnNames, paramNames);

        var orderParam = new OracleDynamicParameters();

        var propertyName = "";
        if (property != null)
        {
            //有鍵值時新增輸出外健語法
            propertyName = property.Name;
            var propertyColumnName = Resolvers.Column(property, sqlBuilder, false);
            insertSql += $" RETURNING {propertyColumnName} INTO :{propertyName} ";
            orderParam.Add(
                property.Name,
                dbType: OracleMappingType.Int32,
                direction: ParameterDirection.Output);
        }

        try
        {
            var paramDictionary = SystemHelper.EntityToDictionary(entity);

            for (var i = 0; i < columnNames.Length; i++)
            {
                var paramName = paramNames[i].Replace(":", "");
                var paramVal = paramDictionary[paramName] ?? null;

                //帶入數值
                orderParam.Add(paramName, paramVal);
            }

            //執行語法
            var sqlResult = _dapperDbConnection.ExecuteScalarAsync(insertSql, orderParam);
            var resultStatus = sqlResult.IsCompletedSuccessfully;

            if (resultStatus)
            {
                //新增成功
                returnData = property != null ? orderParam.Get<int>(propertyName) : 1;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return returnData;
    }

    /// <summary>
    /// 取得SqlBuilder
    /// </summary>
    /// <returns></returns>
    private ISqlBuilder GetSqlBuilder()
    {
        return DommelMapper.GetSqlBuilder(_dapperDbConnection);
    }

    /// <summary>
    /// 轉換order by 字符
    /// </summary>
    /// <param name="orderVal"></param>
    /// <returns></returns>
    private string GetOrderVal(string? orderVal)
    {
        if (string.IsNullOrEmpty(orderVal))
        {
            return "ASC";
        }

        orderVal = orderVal.ToUpper();

        return orderVal.Equals("ASC") ? "ASC" : "DESC";
    }
}