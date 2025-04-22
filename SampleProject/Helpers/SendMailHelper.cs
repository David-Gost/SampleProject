using System.Net;
using System.Net.Mail;
using SampleProject.Models.Custom.Mail;

namespace SampleProject.Helpers;

public static class SendMailHelper
{
    /// <summary>
    /// 發送信件
    /// </summary>
    /// <param name="mailSetting"></param>
    /// <param name="mailContent"></param>
    /// <returns></returns>
    public static SendResult Send(MailSetting mailSetting, MailContent mailContent)
    {
        ArgumentNullException.ThrowIfNull(mailContent);

        var client = new SmtpClient(mailSetting.smtpHost, mailSetting.smtpPort);
        client.UseDefaultCredentials = false;
        client.Credentials = new NetworkCredential(mailSetting.smtpUserName, mailSetting.smtpPassword);
        client.EnableSsl = mailSetting.smtpEnableSsl;

        var mailMessage = new MailMessage();

        mailMessage.From = mailSetting.fromMailAddress;
        mailMessage.Subject = mailContent.subject;
        mailMessage.IsBodyHtml = mailContent.contentIsHtml;
        mailMessage.Body = mailContent.content;

        var sendStatus = true;
        var resultMessage = "";

        //收件者
        var toMailAddress = mailContent.toMailAddress;
        if (toMailAddress is { Count: > 0 })
        {
            foreach (var toAddress in mailContent.toMailAddress)
            {
                mailMessage.To.Add(toAddress);
            }
        }
        else
        {
            sendStatus = false;
            resultMessage = "To mail address is empty!";
        }

        //cc
        var ccMailAddress = mailContent.ccMailAddress;
        if (ccMailAddress is { Count: > 0 })
        {
            foreach (var mailAddress in ccMailAddress)
            {
                mailMessage.CC.Add(mailAddress);
            }
        }

        //bcc
        var bccMailAddress = mailContent.bccMailAddress;
        if (bccMailAddress is { Count: > 0 })
        {
            foreach (var mailAddress in bccMailAddress)
            {
                mailMessage.Bcc.Add(mailAddress);
            }
        }

        //附檔
        var attachments = mailContent.attachments;
        if (attachments is { Count: > 0 })
        {
            foreach (var mailDataAttachment in attachments)
            {
                mailMessage.Attachments.Add(mailDataAttachment);
            }
        }

        if (!sendStatus)
        {
            return new SendResult { sendStatus = sendStatus, message = resultMessage };
        }

        try
        {
            client.Send(mailMessage);
            sendStatus = true;
        }
        catch (Exception e)
        {
            resultMessage = e.Message;
            sendStatus = false;
        }

        return new SendResult { sendStatus = sendStatus, message = resultMessage };
    }
}