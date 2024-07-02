using System.Net.Mail;

namespace SampleProject.Models.Custom.Mail;

/// <summary>
/// smtp設定資料
/// </summary>
public class MailSetting
{
    /// <summary>
    /// smtp host
    /// </summary>
    public string smtpHost;

    /// <summary>
    /// smtp 帳號
    /// </summary>
    public string smtpUserName;

    /// <summary>
    /// smtp 密碼
    /// </summary>
    public string smtpPassword;

    /// <summary>
    /// smtp port
    /// </summary>
    public int smtpPort;

    /// <summary>
    /// smtp 是否啟用ssl
    /// </summary>
    public bool smtpEnableSsl = false;

    /// <summary>
    /// 寄件者資料
    /// </summary>
    public MailAddress fromMailAddress;
}