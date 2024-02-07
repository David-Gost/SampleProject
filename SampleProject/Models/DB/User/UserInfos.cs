using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SampleProject.Models.DB.User;

/// <summary>
/// 使用者資料
/// </summary>
[Table("USER_INFOS")] 
public class UserInfos 
{
  [Key]
  [Column("USER_INFO_ID")]
  public int userInfoId { get; set;}

  /// <summary>
  /// 關聯使用者id
  /// </summary>
  ///
  [Column("USER_ID")]
  [ForeignKey(nameof(userId))]
  public int userId { get; set;}

  /// <summary>
  /// 手機號碼
  /// </summary>
  [Column("TELEPHONE")]
  public string telephone { get; set;}

  /// <summary>
  /// 生日
  /// </summary>
  [Column("BIRTHDAY")]
  public DateTime birthday { get; set;}

}
