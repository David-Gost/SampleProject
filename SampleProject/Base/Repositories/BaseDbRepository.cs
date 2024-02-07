using System.Data;
using System.Dynamic;
using System.Text;
using System.Text.Json;
using Dapper;
using Dapper.Oracle;
using Dommel;
using Oracle.ManagedDataAccess.Client;
using SampleProject.Base.Util;
using SampleProject.Helpers;

namespace SampleProject.Base.Repositories;

/// <summary>
/// DbRepository核心
/// </summary>
public class BaseDbRepository : BaseDbConnection
{
    protected IDbConnection _dbConnection { set; get; }

    protected BaseDbRepository(IConfiguration configuration) : base(configuration)
    {
    }

    /// <summary>
    /// 使用Dapper的擴充套件Dommel撰寫的oracle專用新增資料語法
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns>新增成功回傳1，失敗回應0，有設定自動序列的鍵值時會回傳新增後的序列值</returns>
    protected int InsertIntoOracle<TEntity>(TEntity entity)
    {
        _dbConnection.Open();
        var returnData = 0;
        var dataType = typeof(TEntity);
        var sqlBuilder = DommelMapper.GetSqlBuilder(_dbConnection);

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
            .Except(keyProperties.Where(p => p.IsGenerated).Select(p => p.Property));

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

        using var dbTransaction = _dbConnection.BeginTransaction();
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
            var sqlResult = _dbConnection.ExecuteScalarAsync(insertSql, orderParam);
            dbTransaction.Commit();
            var resultStatus = sqlResult.IsCompletedSuccessfully;

            if (resultStatus)
            {
                //新增成功
                returnData = property != null ? orderParam.Get<int>(propertyName) : 1;
            }
        }
        catch (Exception e)
        {
            dbTransaction.Rollback();
            throw;
        }

        return returnData;
    }
}