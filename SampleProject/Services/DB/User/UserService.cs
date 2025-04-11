using System.Dynamic;
using System.Text.Json;
using Base.Helpers;
using Base.Services;
using SampleProject.Helpers;
using SampleProject.Models.Custom.RequestFrom.User;
using SampleProject.Models.DB.User;
using SampleProject.Repositories.DB.User;


namespace SampleProject.Services.DB.User;

/// <summary>
/// 測試用
/// </summary>
public class UserService 
{
    private readonly UserRepository _userRepository;

    public UserService(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// 取得單筆會員資料
    /// </summary>
    /// <param name="inputData"></param>
    /// <returns></returns>
    public object GetUserData(GetUserDataParam inputData)
    {
        var resultData = SystemHelper.AnyObjToDictionary(_userRepository.GetUserData(inputData));

        //輸出資料轉換
        ReUserData(ref resultData);
        return resultData;
    }

    /// <summary>
    /// 取得多筆User資料
    /// </summary>
    /// <param name="inputData"></param>
    /// <returns></returns>
    public object GetUserDatas(GetUserDataParam inputData)
    {
        var resultDatas = SystemHelper.BaseReList(_userRepository.GetUserDatas(inputData));
        foreach (var resultData in resultDatas)
        {
            object? userData = resultData;
            // ReUserData(ref userData);
        }

        return resultDatas;
    }

    public object AddUserData(ExpandoObject inputData)
    {
        var userData = new Users
        {
            account = "123",
            password = "345"
        };
        var resultData = SystemHelper.AnyObjToDictionary(_userRepository.AddUserData(userData));
        ReUserData(ref resultData);
        return resultData;
    }

    /// <summary>
    /// 洗顯示資料
    /// </summary>
    /// <param name="userData"></param>
    private void ReUserData(ref ExpandoObject? userData)
    {
        if (userData == null)
        {
            return;
        }

        dynamic cacheData = userData;
        const string dateFormat = "yyyy-MM-d HH:mm";

        var createdAt = cacheData.createdAt ?? "";
        var updatedAt = cacheData.updatedAt ?? "";

        IDictionary<string, object> dictionaryData = cacheData;

        //移除不顯示的欄位
        dictionaryData.Remove("password");

        //更改日期格式
        cacheData.createdAt = createdAt.ToString(dateFormat);
        cacheData.updatedAt = updatedAt.ToString(dateFormat);
    }
}