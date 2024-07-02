using System.Net.Mail;

namespace SampleProject.Models.Custom.Mail;

/// <summary>
/// 信件內容資料
/// </summary>
public class MailContent
{
    /// <summary>
    /// 信件主旨
    /// </summary>
    public string? subject = "";

    /// <summary>
    /// 信件內容是否為Html
    /// </summary>
    public bool contentIsHtml = false;

    /// <summary>
    /// 信件內容
    /// </summary>
    public string content = "";

    /// <summary>
    /// 收件者資料名細
    /// </summary>
    public List<MailAddress> toMailAddress;

    /// <summary>
    /// cc資料名細
    /// </summary>
    public List<MailAddress> ccMailAddress;

    /// <summary>
    /// bcc資料名細
    /// </summary>
    public List<MailAddress> bccMailAddress;

    /// <summary>
    /// 附檔明細
    /// </summary>
    public List<Attachment> attachments;
}