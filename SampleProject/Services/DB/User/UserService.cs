using System.Dynamic;
using SampleProject.Helpers;
using SampleProject.Models.Custom.RequestFrom.User;
using SampleProject.Repositories.DB;
using SampleProject.Services.Base;

namespace SampleProject.Services.DB.User;

public class UserService : BaseDbService
{
    private readonly UserRepository _userRepository;

    public UserService(IConfiguration configuration) : base(configuration)
    {
        _userRepository = new UserRepository(_configuration);
    }

    /// <summary>
    /// 取得單筆會員資料
    /// </summary>
    /// <param name="inputData"></param>
    /// <returns></returns>
    public object GetUserData(GetUserDataParam inputData)
    {
        var resultData = SystemHelper.BaseReData(_userRepository.GetUserData(inputData));
        
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
        foreach (var resultData in  resultDatas)
        {
            var userData = resultData;
            ReUserData(ref userData);
        }
        return resultDatas;
    }

    /// <summary>
    /// 洗顯示資料
    /// </summary>
    /// <param name="userData"></param>
    private void ReUserData(ref IDictionary<string, object> userData)
    {
        dynamic cacheData = (ExpandoObject)userData;
        const string dateFormat = "yyyy-MM-d HH:mm";

        var createdAt = cacheData.createdAt ?? "";
        var updatedAt = cacheData.updatedAt ?? "";
        
        //移除不顯示的欄位
        userData.Remove("password");
        
        //更改日期格式
        cacheData.createdAt = createdAt.ToString(dateFormat);
        cacheData.updatedAt = updatedAt.ToString(dateFormat);
    }
}