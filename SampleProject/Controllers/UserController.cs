using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using SampleProject.Controllers.Base;
using SampleProject.Models.Custom.RequestFrom.User;
using SampleProject.Services.DB.Common;
using SampleProject.Services.DB.User;

namespace SampleProject.Controllers;

/// <summary>
/// User相關
/// </summary>
[Route("api/[controller]")]
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
}