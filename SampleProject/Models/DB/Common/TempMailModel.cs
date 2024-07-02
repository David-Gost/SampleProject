using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
using Dommel.Json;
using Newtonsoft.Json;
using SampleProject.Util;

namespace SampleProject.Models.DB.Common;

[Table("temp_mail")]
public class TempMailModel
{
    
    [Column("id")]
    public int id { get; set; }
    
    /// <summary>
    /// 信件資料
    /// </summary>
    [Column("mail_data")]
    [JsonData]
    public ExpandoObject? mailData { get; set; }

    /// <summary>
    /// 發送次數
    /// </summary>
    [Column("send_count")]
    public int sendCount { get; set; }
    
    /// <summary>
    /// 寄件狀態
    /// </summary>
    [Column("send_status")]
    public int sendStatus { get; set; }
    
    /// <summary>
    /// 最後信件錯誤訊息
    /// </summary>
    [Column("last_error_message")]
    public string lastErrorMessage { get; set; }
    
    [Column("create_at")]
    public DateTime? createAt { get; set; }
    
    [Column("update_at")]
    public DateTime? updateAt { get; set; }
}