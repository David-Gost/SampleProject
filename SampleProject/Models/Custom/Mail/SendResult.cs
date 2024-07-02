namespace SampleProject.Models.Custom.Mail;

/// <summary>
/// 發送結果
/// </summary>
public class SendResult
{
 
    /// <summary>
    /// 發送狀態
    /// </summary>
    public bool sendStatus { get; set; } = false;

    /// <summary>
    /// 訊息
    /// </summary>
    public string message { get; set; } = "";
}