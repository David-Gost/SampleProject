using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace SampleProject.Middleware;

/// <summary>
/// Url Path 認證機制
/// </summary>
public class UrlPathAuthMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var middlewareAuthConfig = configuration.GetSection("SystemOption:MiddlewareAuthConfig");

        var isOn = middlewareAuthConfig.GetValue("IsOn", false);

        if (!isOn)
        {
            
            await next(context);
            return;
        }

        var authInfo = middlewareAuthConfig.GetSection("AuthInfo");

        var headers = context.Request.Headers;

        var authStatus = false;
        if (headers.TryGetValue("Authorization", out var authStr))
        {
            var authHeader = authStr.ToString();

            var baseAuthInfo = AuthenticationHeaderValue.Parse(authHeader);

            var authScheme = baseAuthInfo.Scheme;
            var authParameter = baseAuthInfo.Parameter ?? "";

            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authParameter)).Split(':');

            //取出設定檔內容
            var validAccount = authInfo.GetValue<string>("Account", "");
            var passwordInfo = authInfo.GetSection("PasswordInfo");

            //是否為動態密碼，為動態密碼時，密碼為當下時間（ex:目前為下午3點33分，密碼為1533）
            var isDynamic = passwordInfo.GetValue("IsDynamic", true);
            var validPassword =
                isDynamic ? DateTime.Now.ToString("HHm") : passwordInfo.GetValue<string>("Password", "");

            var username = credentials.First();
            var password = credentials.Last();

            //驗證帳密
            if (authScheme.Equals("Basic") &&
                username == validAccount &&
                password == validPassword)
            {
                //建立Client User
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, "gust")
                };
                var identity = new ClaimsIdentity(claims, authScheme);
                var principal = new ClaimsPrincipal(identity);

                context.User = principal;
                authStatus = true;
            }
        }

        if (authStatus)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = 401;
        context.Response.Headers.WWWAuthenticate = "Basic";
    }
}