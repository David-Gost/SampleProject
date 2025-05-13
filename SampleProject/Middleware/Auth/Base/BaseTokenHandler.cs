using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SampleProject.Services.DB.User;

namespace SampleProject.Middleware.Auth.Base;

public class BaseTokenHandler : AuthenticationHandler<BaseSchemeOptions>
{
    private readonly IOptionsMonitor<BaseSchemeOptions> _options;
    private readonly ILoggerFactory _logger;
    private readonly UrlEncoder _encoder;
    private readonly IConfiguration _configuration;
    private readonly AuthUserService _authUserService;

    public BaseTokenHandler(IOptionsMonitor<BaseSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
        IConfiguration configuration, AuthUserService authUserService) : base(options, logger, encoder)
    {
        _options = options;
        _logger = logger;
        _encoder = encoder;
        _configuration = configuration;
        _authUserService = authUserService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        AuthenticateResult authResult = null;
        var checkStep = true;
        var endpoint = Context.GetEndpoint();
        
        var allowAnonymous = endpoint?.Metadata.GetMetadata<IAllowAnonymous>();
        var authorizeAttribute = endpoint?.Metadata.GetMetadata<AuthorizeAttribute>();
        
        //檢查到有AllowAnonymous時，不認證
        if (authorizeAttribute == null || allowAnonymous != null)
        {
            authResult = AuthenticateResult.NoResult();
            await HandleChallengeAsync(new AuthenticationProperties()).ConfigureAwait(false);
            return authResult;
        }

        var authorizationHeader = "";
        var headers = Request.Headers;
        if (headers?.ContainsKey("Authorization") == false)
        {
            checkStep = false;
            authResult = AuthenticateResult.Fail("Authorization header not found.");
        }
        else
        {
            authorizationHeader = headers!["Authorization"];
        }

        var token = "";
        if (string.IsNullOrEmpty(authorizationHeader) ||
            !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            checkStep = false;
            authResult = AuthenticateResult.Fail("Authorization header is empty.");
        }
        else
        {
            token = authorizationHeader["Bearer ".Length..].Trim();
        }

        if (checkStep && string.IsNullOrEmpty(token))
        {
            checkStep = false;
            authResult = AuthenticateResult.Fail("Token is empty.");
        }

        if (checkStep)
        {
            try
            {
                authResult = await ValidateToken(token);
            }
            catch (Exception ex)
            {
                authResult = AuthenticateResult.Fail($"Authentication failed: {ex.Message}");
            }
        }

        //訊息
        var message = authResult!.Succeeded ? "Authentication successful." : authResult.Failure?.Message;
        await HandleChallengeAsync(new AuthenticationProperties()).ConfigureAwait(false);
        return authResult;
    }

    /// <summary>
    /// 檢查Token正確性
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    private async Task<AuthenticateResult> ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtSettings = _configuration.GetSection("SystemOption").GetSection("JwtSettings");
        var secretKey = jwtSettings.GetValue<string>("Secret", "")!;
        var key = System.Text.Encoding.ASCII.GetBytes(secretKey);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var tokenType = jwtToken.Claims.First(x => x.Type == "TokenType").Value;

            if (tokenType == "RefreshToken")
            {
                return AuthenticateResult.Fail("Invalid token type.");
            }

            //驗證accessToken是否有效
            // var tokenValidResult = await _authUserService.ValidAccessToken(token);
            // if (tokenValidResult.statusCode == 0)
            // {
            //     return AuthenticateResult.Fail("Token is invalid.");
            // }

            var identity = new ClaimsIdentity(principal.Claims, Scheme.Name);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (SecurityTokenExpiredException)
        {
            return AuthenticateResult.Fail("Token has expired.");
        }
        catch (SecurityTokenException)
        {
            return AuthenticateResult.Fail("Invalid token.");
        }
    }
}