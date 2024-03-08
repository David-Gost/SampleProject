using System.Dynamic;
using Microsoft.AspNetCore.Mvc;
using SampleProject.Base.Controllers;
using SampleProject.Base.Models.File;
using SampleProject.Helpers;
using SampleProject.Models.Custom.RequestFrom.User;
using SampleProject.Services.DB.User;

namespace SampleProject.Controllers;

/// <summary>
/// User相關
/// </summary>
[Route("Api/[controller]")]
[ApiController]
public class UserController : BaseApiController
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// 取得單筆User資料
    /// </summary>
    /// <param name="inputData"></param>
    /// <returns></returns>
    /// <response code="200">查詢成功</response>
    /// <response code="404">查無資料</response>
    [HttpPost("GetUserData")]
    public Task<ActionResult> GetUserData([FromBody] GetUserDataParam inputData)
    {
        var crontabTasksData = _userService.GetUserData(inputData);

        return Task.FromResult(BackCall(crontabTasksData));
    }

    /// <summary>
    /// 取得單筆User資料
    /// </summary>
    /// <param name="inputData"></param>
    /// <returns></returns>
    [HttpPost("GetUserDatas")]
    public Task<ActionResult> GetUserDatas([FromBody] GetUserDataParam inputData)
    {
        var crontabTasksDatas = _userService.GetUserDatas(inputData);

        return Task.FromResult(BackCall(crontabTasksDatas));
    }

    /// <summary>
    /// 新增資料測試
    /// </summary>
    /// <returns></returns>
    [HttpPost("InsertUserData")]
    public Task<ObjectResult> InsertUserData()
    {
        dynamic insertData = new ExpandoObject();
        insertData.account = "";
        var resultData = _userService.AddUserData(insertData);
        return Task.FromResult(BackCall(resultData));
    }

    /// <summary>
    /// 檔案上傳
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    [HttpPost("UploadFile")]
    public Task<ActionResult> UploadFile(IFormFile file)
    {
        var request = HttpContext.Request;
        var domainName = request.Host.Value;
        var scheme = request.Scheme;
        
        var fileOption = new UploadOptionModel
        {
            uploadPath = "image",
            baseUrl = $"{scheme}://{domainName}"
        };
        return Task.FromResult(BackCall(FileHelper.BaseUploadFile(file,fileOption)!));
    }
}