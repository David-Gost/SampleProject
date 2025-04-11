using Base.Controllers;
using Microsoft.AspNetCore.Mvc;
using Base.Models.Response;
using SampleProject.Models.Custom.RequestFrom.Auth;
using SampleProject.Services.DB.User;

namespace SampleProject.Controllers;

/// <summary>
/// 驗證、登入相關
/// </summary>
public class AuthController : BaseApiController
{
    private readonly AuthUserService _authUserService;

    public AuthController(AuthUserService authUserService)
    {
        _authUserService = authUserService;
    }

    /// <summary>
    /// 登入
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("Login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _authUserService.Login(request);
        var tokenData = result.resultData;
        var httpCode = 200;
        var messageType = MessageType.SUCCESS;

        if (tokenData != null) return BackCall(result.resultData!, result.messages, messageType, httpCode);
        httpCode = 401;
        messageType = MessageType.REQUEST_ERROR;

        return BackCall(result.resultData!, result.messages, messageType, httpCode);
    }

    /// <summary>
    /// 交換金鑰
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("ReAuth")]
    public async Task<IActionResult> ReAuth(ReAuthRequest request)

    {
        var result = await _authUserService.ReAuth(request.refreshToken);
        var tokenData = result.resultData;
        var httpCode = 200;
        var messageType = MessageType.SUCCESS;

        if (tokenData!= null) return BackCall(result.resultData!, result.messages, messageType, httpCode);
        httpCode = 401;
        messageType = MessageType.REQUEST_ERROR;

        return BackCall(result.resultData!, result.messages, messageType, httpCode);
    }
}