namespace Base.Models.Http.AuthType;

/// <summary>
/// Digest驗證
/// </summary>
public class DigestAuthModel: AuthModel
{
    public override string authType => "Digest";
    
    /// <summary>
    /// 使用者名稱
    /// </summary>
    public string userName { get; set; }
    
    /// <summary>
    /// 密碼
    /// </summary>
    public string password { get; set; }
}