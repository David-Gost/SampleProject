using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Dommel.Json;
using SampleProject.Base.Interface.DB;

namespace SampleProject.Models.DB.Common;

[Table("temp_mail", Schema = "public")]
public class TempMailModel : ITimestampable
{
    [Key] [Column("id")] public int id { get; set; }

    /// <summary>
    /// 信件資料
    /// </summary>
    [Column("mail_data", TypeName = "jsonb")]
    [JsonData]
    public JsonDocument? mailData { get; set; }

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

    [Column("created_at")] public DateTime? createdAt { get; set; }

    [Column("updated_at")] public DateTime? updatedAt { get; set; }
}