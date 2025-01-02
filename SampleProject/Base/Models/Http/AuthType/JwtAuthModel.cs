namespace SampleProject.Base.Models.Http.AuthType;

/// <summary>
/// JWT 驗證
/// </summary>
public class JwtAuthModel : AuthModel
{
    public override string authType => "JWT";

    /// <summary>
    /// auth前綴
    /// </summary>
    public string requestHeaderPrefix { get; set; } = "Bearer";
    
    /// <summary>
    /// Token 值
    /// </summary>
    public string token { get; set; }

}