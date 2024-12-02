using SampleProject.Base.Models.ServiceResult;
using SampleProject.Models.Custom.RequestFrom.Auth;
using SampleProject.Models.DB.User;
using SampleProject.Repositories.DB.User;

namespace SampleProject.Services.DB.User;

public class AuthUserService
{
    private readonly AuthUserRepository _authUserRepository;

    public AuthUserService(AuthUserRepository authUserRepository)
    {
        _authUserRepository = authUserRepository;
    }

    /// <summary>
    /// 登入
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<BaseResult> Login(LoginRequest request)
    {
        var userData = await _authUserRepository.Login(request);

        var dataResult = new BaseResult();

        var message = "";

        if (userData == null)
        {
            message = "帳號或密碼錯誤";
        }
        else
        {
            message = "登入成功！";
            dataResult.resultData = new AuthTokenResult()
            {
                accessToken = userData!.accessToken ?? "",
                refreshToken = userData.refreshToken ?? "",
            };
        }

        dataResult.messages.Add(message);


        return dataResult;
    }

    /// <summary>
    /// 交換金鑰
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    public async Task<BaseResult> ReAuth(string refreshToken)
    {
        var userData = await _authUserRepository.ReAuth(refreshToken);

        var dataResult = new BaseResult();
        var message = "";

        if (userData == null)
        {
            message = "Token 無效或已過期.";
        }
        else
        {
            message = "success";
            dataResult.resultData = new AuthTokenResult()
            {
                accessToken = userData!.accessToken ?? "",
                refreshToken = userData.refreshToken ?? "",
            };
        }
        dataResult.messages.Add(message);
        return dataResult;
    }

    /// <summary>
    /// 驗證登入資料
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public string ValidLoginParams(LoginRequest request)
    {
        List<string> message = [];

        if (string.IsNullOrEmpty(request.account))
        {
            message.Add("Account is required.");
        }

        if (string.IsNullOrEmpty(request.password))
        {
            message.Add("Password is required.");
        }

        return string.Join(", ", message);
    }
}