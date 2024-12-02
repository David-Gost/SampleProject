using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace SampleProject.Middleware.Auth.Base;

public class BaseTokenHandler : AuthenticationHandler<BaseSchemeOptions>
{
    private readonly IOptionsMonitor<BaseSchemeOptions> _options;
    private readonly ILoggerFactory _logger;
    private readonly UrlEncoder _encoder;
    private readonly IConfiguration _configuration;

    public BaseTokenHandler(IOptionsMonitor<BaseSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
        IConfiguration configuration) : base(options, logger, encoder)
    {
        _options = options;
        _logger = logger;
        _encoder = encoder;
        _configuration = configuration;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        AuthenticateResult authResult = null;
        var checkStep = true;
        var endpoint = Context.GetEndpoint();
        var allowAnonymous = endpoint?.Metadata.GetMetadata<IAllowAnonymous>();

        //檢查到有AllowAnonymous時，不認證
        if (allowAnonymous != null)
        {
            checkStep = false;
            authResult = AuthenticateResult.NoResult();
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

        if (string.IsNullOrEmpty(token))
        {
            checkStep = false;
            authResult = AuthenticateResult.Fail("Token is empty.");
        }

        if (checkStep)
        {
            try
            {
                authResult = ValidateToken(token);
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
    private AuthenticateResult ValidateToken(string token)
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