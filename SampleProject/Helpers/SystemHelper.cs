using System.Dynamic;

namespace SampleProject.Helpers;

/// <summary>
/// 系統函示（靜態類工具）
/// </summary>
public static class SystemHelper
{
    /// <summary>
    /// 基本多筆資料轉換
    /// </summary>
    /// <param name="fromList"></param>
    /// <returns></returns>
    public static IEnumerable<IDictionary<string, object>> BaseReList(IEnumerable<object> fromList)
    {
        return fromList.Select((fromData, index) => new { fromData, index })
            .Select(item => BaseReData((IDictionary<string, object>)item.fromData))
            .ToList();
    }

    /// <summary>
    /// 基本單筆資料轉換
    /// </summary>
    /// <param name="fromData"></param>
    /// <returns></returns>
    public static IDictionary<string, object> BaseReData(IDictionary<string, object>? fromData)
    {
        if (fromData == null)
        {
            return (new object() as IDictionary<string, object>)!;
        }

        var returnData = new ExpandoObject() as IDictionary<string, object>;
        foreach (var fromDataKey in fromData.Keys)
        {
            var dataKeyName = ToCamelCase(fromDataKey);
            var dataVal = fromData[fromDataKey];
            returnData.Add(dataKeyName, dataVal);
        }

        return returnData;
    }

    /// <summary>
    /// 小駝峰文字轉換
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            //空值，回應空字串
            return str;
        }

        var words = str.Split('_');
        var wordsLength = words.Length;

        if (wordsLength == 1)
        {
            //只有一個字詞，無條件轉小寫
            return str.ToLowerInvariant();
        }

        //有一個字詞以上
        for (var i = 0; i < words.Length; i++)
        {
            var wordStr = words[i];
            var wordStrLength = wordStr.Length;
            if (i == 0)
            {
                //首個字詞全小寫
                words[i] = wordStr.ToLower();
            }
            else
            {
                //後續連接字詞首個字母大寫，但字詞長度小於2時無條件轉小寫
                words[i] = char.ToUpper(wordStr[0]) + wordStr[1..].ToLower();
            }
        }

        return string.Join("", words);
    }

    /// <summary>
    /// 任一物件轉換為字典
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static ExpandoObject AnyObjToDictionary(object? obj)
    {
        if (obj == null)
        {
            return new ExpandoObject();
        }

        var expandoObj = new ExpandoObject();
        var propertyInfos = obj.GetType().GetProperties();

        foreach (var propertyInfo in propertyInfos)
        {
            var columnName = propertyInfo.Name;

            ((IDictionary<string, object?>)expandoObj).Add(columnName, propertyInfo.GetValue(obj));
        }

        return expandoObj;
    }

    /// <summary>
    /// entity轉換成字典
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public static Dictionary<string, object?> EntityToDictionary<TEntity>(TEntity entity)
    {
        return typeof(TEntity).GetProperties()
            .ToDictionary(property => property.Name, property => property.GetValue(entity, null));
    }
}