using System.Xml.Linq;
using Microsoft.AspNetCore.Http.Features;
using Newtonsoft.Json;
using SampleProject.Models.Custom.Log;

namespace SampleProject.Services.Custom.Log;

public class RequestLoggerService
{
    private readonly string _logPath;
    private readonly int _retentionDays;
    private readonly IWebHostEnvironment _env;

    public RequestLoggerService(IConfiguration configuration, IWebHostEnvironment env)
    {
        var systemOptionDictionary = configuration.GetSection("SystemOption").GetChildren().ToDictionary(x => x.Key);
        var requestLogConfig = systemOptionDictionary.GetValueOrDefault("RequestLog");
        var configPath = requestLogConfig?.GetValue<string>("LogPath", "logs");
        _logPath = Path.IsPathRooted(configPath)
            ? configPath
            : Path.Combine(env.WebRootPath, configPath ?? "logs");

        if (!Directory.Exists(_logPath))
        {
            Directory.CreateDirectory(_logPath);
        }

        _retentionDays = requestLogConfig?.GetValue("RetentionDays", 90) ?? 90;
        _env = env;
    }

    /// <summary>
    /// 紀錄自訂錯誤內容
    /// </summary>
    /// <param name="error"></param>
    /// <returns></returns>
    public Task LogAsync(ErrorLoggerModel error)
    {
        WriteError(error.httpContext, error.exception, error.statusCode, error.requestBody);
        return Task.CompletedTask;
    }

    public Task WriteError(HttpContext context, Exception? ex, int statusCode, string? requestBody = null)
    {
        ArgumentNullException.ThrowIfNull(requestBody);
        CleanupOldLogs();

        var doc = BuildErrorDocument(context, ex, statusCode, requestBody);

        var fileName = $"{DateTimeOffset.Now:yyyy-MM-dd_HH-mm-ss_zzz}_{Guid.NewGuid()}.xml"
            .Replace(":", "-"); // Windows 不允許冒號

        var filePath = Path.Combine(_logPath, fileName);

        doc.Save(filePath);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 主要方法：寫入 Log XML
    /// </summary>
    public async Task WriteError(HttpContext context, Exception? ex, int statusCode)
    {
        //清理舊檔案
        CleanupOldLogs();

        var requestBody = "";
        if (context.Request.Body.CanSeek)
        {
            context.Request.Body.Seek(0, SeekOrigin.Begin);

            if (context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
            {
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
            }
            else if (context.Request.HasFormContentType)
            {
                try
                {
                    var form = await context.Request.ReadFormAsync();
                    var formDict = form.ToDictionary(x => x.Key, x => x.Value.ToString());
                    requestBody = JsonConvert.SerializeObject(formDict);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            // 將流的位置重置為開頭，以防其他地方需要讀取
            context.Request.Body.Seek(0, SeekOrigin.Begin);
        }

        var doc = BuildErrorDocument(context, ex, statusCode, requestBody);

        var fileName = $"{DateTimeOffset.Now:yyyy-MM-dd_HH-mm-ss_zzz}_{Guid.NewGuid()}.xml"
            .Replace(":", "-"); // Windows 不允許冒號

        var filePath = Path.Combine(_logPath, fileName);

        await Task.Run(() => doc.Save(filePath));
    }

    private XDocument BuildErrorDocument(
        HttpContext context,
        Exception? ex,
        int statusCode,
        string? requestBody)
    {
        var errorId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.Now;

        var auth = context.User?.Identity?.IsAuthenticated ?? false;
        var userName = auth ? context.User?.Identity?.Name ?? "anonymous" : "anonymous";
        var exceptionDetail = ex?.ToString() ?? "";
        var (messageLogElement, exceptionMessage) = WriteMessageLog(statusCode, context, ex);

        var doc = new XDocument(
            new XElement("error",
                new XAttribute("errorId", errorId),
                new XAttribute("application",
                    context.RequestServices.GetRequiredService<IWebHostEnvironment>().ApplicationName),
                new XAttribute("user", userName),
                new XAttribute("host", Environment.MachineName),
                new XAttribute("type", ex != null ? ex.GetType().Name : "HTTP"),
                new XAttribute("message", ex?.Message ?? exceptionMessage),
                new XAttribute("detail", exceptionDetail),
                new XAttribute("time", now.ToString("yyyy-MM-dd HH:mm:ss zzz")),
                new XAttribute("statusCode", statusCode),
                WriteServerVariables(context, statusCode),
                WriteIdentityUser(context),
                messageLogElement,
                WriteRequestBody(requestBody)
            )
        );

        return doc;
    }

    private XElement WriteRequestBody(string? requestBody)
    {
        return new XElement("requestBody",
            new XCData(requestBody ?? "")
        );
    }

    /// <summary>
    /// 取出response
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private string? TryReadResponseBody(HttpContext context)
    {
        // 在中介軟體啟用後，CanSeek 和 CanRead 將會是 true
        if (!context.Response.Body.CanSeek || !context.Response.Body.CanRead || context.Response.Body.Length == 0)
        {
            return null;
        }

        // 將指標移到開頭準備讀取
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var
            reader = new StreamReader(context.Response.Body,
                leaveOpen: true); // leaveOpen: true 很重要，避免 MemoryStream 被釋放
        var responseBody = reader.ReadToEnd();

        // 再次將指標移到開頭，這樣 ASP.NET Core 才能把內容複製回原始串流並傳送給Client
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        return responseBody;
    }

    /// <summary>
    /// 與 ELMAH 相同格式 serverVariables
    /// </summary>
    private XElement WriteServerVariables(HttpContext context, int statusCode)
    {
        var serverVars = new XElement("serverVariables");

        Add("Scheme", context.Request.Scheme);
        Add("Method", context.Request.Method);
        Add("Path", context.Request.Path);
        Add("QueryString", context.Request.QueryString.Value ?? "");
        Add("RawTarget", context.Features.Get<IHttpRequestFeature>()?.RawTarget ?? "");
        Add("HttpVersion", context.Request.Protocol);

        foreach (var (key, headerValue) in context.Request.Headers)
        {
            if (!string.IsNullOrEmpty(headerValue))
            {
                Add($"Header_{key}", headerValue!);
            }
        }

        Add("RemoteIpAddress", context.Connection.RemoteIpAddress?.ToString() ?? "");
        Add("RemotePort", context.Connection.RemotePort.ToString());
        Add("LocalIpAddress", context.Connection.LocalIpAddress?.ToString() ?? "");
        Add("LocalPort", context.Connection.LocalPort.ToString());
        Add("StatusCode", statusCode.ToString());

        return serverVars;

        void Add(string name, string value)
        {
            serverVars.Add(new XElement("item",
                new XAttribute("name", name),
                new XElement("value", new XAttribute("string", value ?? ""))
            ));
        }
    }

    /// <summary>
    /// 儲存身份資訊
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private XElement WriteIdentityUser(HttpContext context)
    {
        var auth = context.User?.Identity?.IsAuthenticated ?? false;
        var userName = auth ? context.User?.Identity?.Name ?? "anonymous" : "anonymous";

        var identityUser = new XElement("identityUser");
        identityUser.Add(new XElement("auth", auth));
        identityUser.Add(new XElement("userName", userName));
        return identityUser;
    }

    /// <summary>
    /// 與 ELMAH 一樣的 messageLog 格式
    /// </summary>
    private (XElement, string) WriteMessageLog(int statusCode, HttpContext context, Exception? exception = null)
    {
        var message = "";

        var responseBodyString = TryReadResponseBody(context);
        if (!string.IsNullOrEmpty(responseBodyString))
        {
            message = responseBodyString;
        }

        if (statusCode == 401 && string.IsNullOrEmpty(message))
        {
            message = "unAuthorized";
        }

        var level = statusCode switch
        {
            >= 200 and < 300 => "Success",
            >= 400 and < 500 => "Warning",
            >= 500 and < 600 => "Error",
            _ => "Information"
        };

        var writerMessage = !(exception != null && exception.GetType() == typeof(HttpRequestException));

        var msgLog = new XElement("messageLog");
        var nowTime = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz");

        if (exception != null)
        {
            msgLog.Add(new XElement("message",
                new XAttribute("level", exception.GetType().Name),
                new XAttribute("time-stamp", nowTime),
                new XAttribute("message", exception.Message)
            ));
        }

        if (!string.IsNullOrEmpty(message) && writerMessage)
        {
            msgLog.Add(new XElement("message",
                new XAttribute("level", level),
                new XAttribute("time-stamp", nowTime),
                new XAttribute("message", message)
            ));
        }

        return (msgLog, message); // <-- 回傳元組
    }

    /// <summary>
    /// 自動刪除X天前的XML檔案
    /// </summary>
    private void CleanupOldLogs()
    {
        var cutoff = DateTime.UtcNow.AddDays(-_retentionDays);

        foreach (var file in Directory.GetFiles(_logPath, "*.xml"))
        {
            if (File.GetCreationTimeUtc(file) < cutoff)
            {
                File.Delete(file);
            }
        }
    }
}