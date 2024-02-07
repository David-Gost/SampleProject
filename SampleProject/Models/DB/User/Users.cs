using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Dommel;
using System;

namespace SampleProject.Models.DB.User;

/// <summary>
/// 使用者主表
/// </summary>
[Table("USERS")] 
public class Users 
{

  [Key]
  [Column("USER_ID")]
  public int userId { get; set;}

  /// <summary>
  /// 帳號
  /// </summary>
  [Column("ACCOUNT")]
  public string account { get; set;}

  /// <summary>
  /// 使用者密碼
  /// </summary>
  [Column("PASSWORD")]
  public string password { get; set;}

  /// <summary>
  /// 最後登入日期
  /// </summary>
  [Column("LAST_LOGIN")]
  public DateTime lastLogin { get; set;}

  /// <summary>
  /// token
  /// </summary>
  [Column("TOKEN")]
  public string token { get; set;}

  /// <summary>
  /// token過期時間
  /// </summary>
  [Column("TOKEN_TIME")]
  public DateTime tokenTime { get; set;}

  /// <summary>
  /// 備註
  /// </summary>
  [Column("REMARK")]
  public string remark { get; set;}

  /// <summary>
  /// 資料建立日期
  /// </summary>
  [Column("CREATED_AT")]
  public DateTime createdAt { get; set;}

  /// <summary>
  /// 資料更新日期
  /// </summary>
  [Column("UPDATED_AT")]
  public DateTime updatedAt { get; set;}

  /// <summary>
  /// 使用者資訊
  /// </summary>
  [ForeignKey(nameof(userId))]
  [Ignore]
  public UserInfos UserInfo { get; set; }
}
