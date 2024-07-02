using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using SampleProject.Base.Controllers;
using SampleProject.Helpers;
using SampleProject.Models.Custom.Mail;
using SampleProject.Models.DB.Common;
using SampleProject.Services.DB.Common;
using SampleProject.Util;

namespace SampleProject.Controllers;

public class TempController : BaseApiController
{
    private readonly TempMailService _tempMailService;
    private readonly IConfiguration _configuration;

    public TempController(TempMailService tempMailService,IConfiguration configuration)
    {
        _tempMailService = tempMailService;
        _configuration = configuration;
    }
    
    [HttpPost("GetDatas")]
    public async Task<IActionResult> GetDatas()
    {
        var filterParams = new Dictionary<string, object>
        {
            // {"filterSendStatus", new List<int> {0}},
        };

        var orderParams = new Dictionary<string, string>
        {
            {"create_at", "ASC"},
        };
        var dataList =  await Task.Run(() => _tempMailService.GetDatas(filterParams, orderParams));
        
        return BackCall(dataList!);
    }

    /// <summary>
    /// 新增資料
    /// </summary>
    /// <returns></returns>
    [HttpPost("InsertData")]
    public async Task<IActionResult> InsertData()
    {
        
        var contentData = new MailContent
        {
            subject = "測試信件",
            content = $"信件內容",
            toMailAddress= [new MailAddress("test@gmail.com", "Test User")],
        };
        
        dynamic obj = SystemHelper.AnyObjToExpandoObject(contentData);
        
        var tempMailModel = new TempMailModel
        {
            mailData = obj,
        };
        
        var result = await Task.Run(() => _tempMailService.InsertData(tempMailModel));
        return BackCall(result!);
    }
}