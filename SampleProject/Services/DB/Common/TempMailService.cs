using System.Dynamic;
using System.Net.Mail;
using SampleProject.Models.DB.Common;
using SampleProject.Repositories.DB.Common;

namespace SampleProject.Services.DB.Common;

public class TempMailService
{
    private readonly TempMailRepository _tempMailRepository;

    public TempMailService(TempMailRepository tempMailRepository)
    {
        _tempMailRepository = tempMailRepository;
    }

    /// <summary>
    /// 取得信件紀錄
    /// </summary>
    /// <param name="filterParams"></param>
    /// <param name="orderParams"></param>
    /// <param name="limit"></param>
    /// <param name="isOnce"></param>
    /// <returns></returns>
    public IEnumerable<TempMailModel>? GetDatas(IDictionary<string, object>? filterParams,
        IDictionary<string, string>? orderParams,
        int limit = 0,
        bool isOnce = false)
    {
        var result = _tempMailRepository.GetDatas(filterParams, orderParams, limit, isOnce).Result;
        return result;
    }

    /// <summary>
    /// 新增mail暫存資料
    /// </summary>
    /// <param name="mailData"></param>
    /// <returns></returns>
    public TempMailModel? InsertData(TempMailModel mailData)
    {
        var resultData = _tempMailRepository.InsertMailData(mailData)?.Result!;
        return resultData;
    }

    /// <summary>
    /// 更新mail暫存資料
    /// </summary>
    /// <param name="tempMailModel"></param>
    /// <returns></returns>
    public TempMailModel? UpdateData(TempMailModel tempMailModel)
    {
        var resultData = _tempMailRepository.UpdateMailData(tempMailModel)?.Result!;
        return resultData;
    }

    /// <summary>
    /// 資料轉換為MailAddress
    /// </summary>
    /// <param name="fromDataList"></param>
    /// <returns></returns>
    public List<MailAddress> DataToMailAddresses(List<object>? fromDataList)
    {
        if (fromDataList == null || fromDataList.Count == 0)
        {
            return [];
        }

        return (from dynamic fromData in fromDataList
            let mail = fromData.Address ?? ""
            let displayName = fromData.DisplayName
            where !string.IsNullOrEmpty(mail)
            select new MailAddress(mail, displayName)).ToList();
    }
}