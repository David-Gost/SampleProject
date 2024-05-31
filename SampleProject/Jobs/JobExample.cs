using System.Linq.Expressions;
using Hangfire;
using Newtonsoft.Json;
using SampleProject.Services.Custom;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SampleProject.Jobs;

/// <summary>
/// 排程Job範例
/// </summary>
public class JobExample
{
    int _counter = 0;

    public JobExample()
    {

    }

    /// <summary>
    /// 設定定期排程工作
    /// </summary>
    public void SetSchTasks()
    {
        SetSchTask("TestEveryMinute", () => TestJob(), "* * * * *");
    }

    /// <summary>
    /// 先刪再設，避免錯過時間排程在伺服器啟動時執行
    /// </summary>
    /// <param name="id">排程代號</param>
    /// <param name="job">實際需執行工作</param>
    /// <param name="cron">排程參數，全*代表每分鐘皆執行，代號分別如下 分 小時 日期 月份 週</param>
    [Obsolete("Obsolete")]
    public void SetSchTask(string id, Expression<Action> job, string cron)
    {
        RecurringJob.RemoveIfExists(id);
        RecurringJob.AddOrUpdate(id, job, cron, TimeZoneInfo.Local);
    }

    public async Task TestJob()
    {
        Console.WriteLine($"Test {_counter++}");
    }
}

/// <summary>
/// 擴充方法，註冊排程工作元件以及設定排程
/// </summary>
public static class SchTaskWorkerExtensions
{
    public static WebApplicationBuilder AddJobExample(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<JobExample>();
        return builder;
    }

    public static void SetJobExample(this WebApplication app)
    {
        var worker = app.Services.GetRequiredService<JobExample>();
        worker.SetSchTasks();
    }
}