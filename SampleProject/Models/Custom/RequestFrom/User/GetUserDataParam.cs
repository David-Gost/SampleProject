using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SampleProject.Models.Custom.RequestFrom.User;

/// <summary>
/// 取得User資料pos參數
/// </summary>
public class GetUserDataParam
{
    /// <summary>
    /// 帳號id索引 ex:122233
    /// </summary>
    public int userId { get; set; }
    
    /// <summary>
    /// 帳號
    /// </summary>
    [DefaultValue("")]
    public string account { get; set; }

}