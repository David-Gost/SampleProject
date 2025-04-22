using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SampleProject.Base.Repositories;
using Base.Util.DB.Dapper;
using SampleProject.Base.Util.DB.EFCore;
using SampleProject.Database;
using SampleProject.Models.Custom.RequestFrom.Auth;
using SampleProject.Models.DB.User;

namespace SampleProject.Repositories.DB.User;

public class AuthUserRepository : BaseDbRepository
{
    private readonly IConfiguration _configuration;

    public AuthUserRepository(DapperContextManager dapperContextManager, DbContextManager dbContextManager,
        IDbConnection dapperDbConnection, ApplicationDbContext efDbConnection, IConfiguration configuration) : base(
        dapperContextManager, dbContextManager, dapperDbConnection, efDbConnection)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// 人員登入
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<AuthUsers?> Login(LoginRequest request)
    {
        AuthUsers? userData = null;
        userData = efDbConnection.UserAuths.FirstOrDefault(e => e!.account == request.account);

        if (userData == null) return userData;

        var validPassword = BCrypt.Net.BCrypt.Verify(request.password, userData.password);
        if (!validPassword)
        {
            userData = null;
        }
        else
        {
            //產生token
            GenerateToken(ref userData);
            efDbConnection.UserAuths.Update(userData);
            await efDbConnection.SaveChangesAsync();
        }

        return userData;
    }

    /// <summary>
    /// 重新產生金鑰
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    public async Task<AuthUsers?> ReAuth(string refreshToken)
    {
        AuthUsers? userData = null;
        userData = efDbConnection.UserAuths.FirstOrDefault(e => e!.refreshToken == refreshToken);

        if (userData == null) return userData;

        GenerateToken(ref userData);
        efDbConnection.UserAuths.Update(userData);
        await efDbConnection.SaveChangesAsync();

        return userData;
    }

    /// <summary>
    /// 新增資料
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public async Task<AuthUsers> Insert(AuthUsers user)
    {
        efDbConnection.UserAuths.Add(user);
        await _efDbConnection.SaveChangesAsync();

        return user;
    }

    /// <summary>
    /// 產生token
    /// </summary>
    /// <param name="userData"></param>
    private void GenerateToken(ref AuthUsers? userData)
    {
        if (userData == null)
        {
            return;
        }

        //jwt相關設定
        var jwtSettings = _configuration.GetSection("SystemOption").GetSection("JwtSettings");
        var secretKey = jwtSettings.GetValue<string>("Secret", "")!;
        var allowMultiClients = jwtSettings.GetValue<bool>("AllowMultipleClients", false);

        // 確保密鑰長度至少為 32 字節
        if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
        {
            throw new InvalidOperationException("JWT secret key must be at least 32 characters long.");
        }

        var key = Encoding.ASCII.GetBytes(secretKey);

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                //加入使用者資訊
                new Claim(ClaimTypes.Name, userData.account!),
                new Claim(ClaimTypes.NameIdentifier, userData.authUserId.ToString()),
                new Claim("TokenType", "AccessToken")
            ]),
            Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpirationInMinutes"])),
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var accessToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

        // 生成 Refresh Token
        var refreshTokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, userData.authUserId.ToString()),
                new Claim("TokenType", "RefreshToken")
            ]),
            Expires = DateTime.UtcNow.AddDays(jwtSettings.GetValue("RefreshTokenExpirationInDays",
                1)), // Refresh Token 有效期設為 7 天
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var refreshToken = tokenHandler.WriteToken(tokenHandler.CreateToken(refreshTokenDescriptor));

        // 將令牌存儲在 userData 中
        userData.accessToken = accessToken;
        userData.refreshToken = refreshToken;
    }
}