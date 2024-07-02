using System.Configuration;
using System.Linq.Expressions;
using System.Net.Mail;
using Hangfire;
using Newtonsoft.Json;
using SampleProject.Models.Custom.Mail;
using SampleProject.Services.DB.Common;
using SampleProject.Util;


namespace SampleProject.Jobs;

/// <summary>
/// 發送信件排程
/// </summary>
public class SendMailJob
{
    private readonly TempMailService _tempMailService;
    private readonly IConfiguration _configuration;

    public SendMailJob(TempMailService tempMailService, IConfiguration configuration)
    {
        _tempMailService = tempMailService;
        _configuration = configuration;
    }

    /// <summary>
    /// 設定定期排程工作
    /// </summary>
    public void SetSchTasks()
    {
        SetSchTask("SendMailHelper", () => SendMail(), "* * * * *");
    }

    /// <summary>
    /// 先刪再設，避免錯過時間排程在伺服器啟動時執行
    /// </summary>
    /// <param name="jobTag">排程代號</param>
    /// <param name="job">實際需執行工作</param>
    /// <param name="cron">排程參數，全*代表每分鐘皆執行，代號分別如下 分 小時 日期 月份 週</param>
    [Obsolete("Obsolete")]
    public void SetSchTask(string jobTag, Expression<Action> job, string cron)
    {
        RecurringJob.RemoveIfExists(jobTag);
        RecurringJob.AddOrUpdate(jobTag, job, cron, TimeZoneInfo.Local);
    }

    public async Task SendMail()
    {
        var filterParams = new Dictionary<string, object>
        {
            { "filterSendStatus", new List<int> { 0 } },
        };

        var orderParams = new Dictionary<string, string>
        {
            { "create_at", "ASC" },
        };

        var dataList = _tempMailService.GetDatas(filterParams, orderParams, 1);

        var result = JsonConvert.SerializeObject(dataList);

        var smtpOption = _configuration.GetSection("SmtpInfo");
        var smtpSettingInfo = smtpOption.GetSection("Setting");
        var fromMailInfo = smtpOption.GetSection("From");

        //smtp設定
        var fromMail = fromMailInfo.GetValue<string>("Address", "") ?? "";

        if (!string.IsNullOrEmpty(fromMail))
        {
            var mailSettingData = new MailSetting
            {
                smtpHost = smtpSettingInfo.GetValue<string>("Host", "") ?? "",
                smtpUserName = smtpSettingInfo.GetValue<string>("Username", "") ?? "",
                smtpPassword = smtpSettingInfo.GetValue<string>("Password", "") ?? "",
                smtpPort = smtpSettingInfo.GetValue<int>("Port", 0),
                smtpEnableSsl = smtpSettingInfo.GetValue<bool>("EnableSsl"),
                fromMailAddress = new MailAddress
                (
                    address: fromMail,
                    displayName: fromMailInfo.GetValue<string>("Name", "") ?? ""
                )
            };

            foreach (var tempMailModel in dataList!)
            {
                dynamic? contentData = tempMailModel.mailData ?? null;
                var mailContentData = new MailContent
                {
                    subject = contentData!.subject ?? "",
                    content = contentData!.content ?? "",
                    contentIsHtml = contentData!.contentIsHtml,
                    ccMailAddress = _tempMailService.DataToMailAddresses(contentData.ccMailAddress),
                    bccMailAddress = _tempMailService.DataToMailAddresses(contentData.bccMailAddress),
                    toMailAddress = _tempMailService.DataToMailAddresses(contentData.toMailAddress)
                };

                //信件發送
                var sendResult = SendMailHelper.Send(mailSettingData, mailContentData);

                var resultStatus = sendResult.sendStatus;
                tempMailModel.sendCount += 1;

                if (resultStatus)
                {
                    tempMailModel.sendStatus = 1;
                    tempMailModel.lastErrorMessage = "";
                }
                else
                {
                    tempMailModel.lastErrorMessage = sendResult.message ?? "";
                }

                //超過5次仍失敗，變更狀態，不再發送
                if (tempMailModel.sendCount == 5)
                {
                    tempMailModel.sendStatus = 9;
                }
                
                //更新紀錄資料
                _tempMailService.UpdateData(tempMailModel);
            }
        }
    }
}

/// <summary>
/// 擴充方法，註冊排程工作元件以及設定排程
/// </summary>
public static class SchSendMailTaskWorkerExtensions
{
    /// <summary>
    /// Service注入
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static WebApplicationBuilder AddSendMailJob(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<SendMailJob>();
        return builder;
    }

    /// <summary>
    /// 加入排程
    /// </summary>
    /// <param name="app"></param>
    public static void SetSendMailJob(this WebApplication app)
    {
        var worker = app.Services.GetRequiredService<SendMailJob>();
        worker.SetSchTasks();
    }
}