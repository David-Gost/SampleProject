using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SampleProject.Models.DB.User;

/// <summary>
/// 使用者主表
/// </summary>
[Table("USERS")] 
public class Users 
{
  [Key]
  public int USER_ID { get; set;}

  /// <summary>
  /// 帳號
  /// </summary>
  public string ACCOUNT { get; set;}

  /// <summary>
  /// 使用者密碼
  /// </summary>
  public string PASSWORD { get; set;}

  /// <summary>
  /// 最後登入日期
  /// </summary>
  public DateTime LAST_LOGIN { get; set;}

  /// <summary>
  /// token
  /// </summary>
  public string TOKEN { get; set;}

  /// <summary>
  /// token過期時間
  /// </summary>
  public DateTime TOKEN_TIME { get; set;}

  /// <summary>
  /// 備註
  /// </summary>
  public string REMARK { get; set;}

  /// <summary>
  /// 資料建立日期
  /// </summary>
  public DateTime CREATE_AT { get; set;}

  /// <summary>
  /// 資料更新日期
  /// </summary>
  public DateTime UPDATE_AT { get; set;}

}
