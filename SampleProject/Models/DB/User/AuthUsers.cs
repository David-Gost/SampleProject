using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SampleProject.Models.DB.User;

/// <summary>
/// 授權使用者列表
/// </summary>
[Table("auth_users", Schema = "test")] 
public class AuthUsers 
{
  
  [Key]
  [Column("auth_user_id")]
  public int authUserId { get; set;}

  /// <summary>
  /// 帳號
  /// </summary>
  [Column("account")]
  public string? account { get; set;}

  /// <summary>
  /// 密碼
  /// </summary>
  [Column("password")]
  public string? password { get; set;}

  /// <summary>
  /// 登入類型
  /// </summary>
  [Column("login_type")]
  public string? loginType { get; set;}

  /// <summary>
  /// 登入token
  /// </summary>
  [Column("access_token")]
  public string? accessToken { get; set;}

  
  [Column("refresh_token")]
  public string? refreshToken { get; set;}

}
