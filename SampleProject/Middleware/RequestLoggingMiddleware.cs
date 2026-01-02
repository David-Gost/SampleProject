using System.Text;
using Newtonsoft.Json;
using SampleProject.Services.Custom.Log;

namespace SampleProject.Middleware;

/// <summary>
/// 暫存request資料
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RequestLoggerService _requestLoggerService;
    private const int MaxResponseLogSize = 1024 * 1024; // 1MB

    public RequestLoggingMiddleware(RequestDelegate next, RequestLoggerService requestLoggerService)
    {
        _next = next;
        _requestLoggerService = requestLoggerService;
    }

    public async Task Invoke(HttpContext context)
    {
        // 跳過特定路徑
        if (context.Request.Path.Value?.Contains(".well-known") == true)
        {
            await _next(context);
            return;
        }

        // 讀取 Request Body
        context.Request.EnableBuffering();
        var requestBody = await ReadRequestBodyAsync(context);
        context.Items["requestBody"] = requestBody;

        // 攔截 Response Body
        var originalBodyStream = context.Response.Body;
        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var responseCopied = false;

        try
        {
            await _next(context);
            responseCopied = true; // 只有成功執行才標記可以 copy

            // 只記錄 JSON 回應
            if (context.Response.ContentType?.Contains("application/json") == true &&
                responseBody.Length <= MaxResponseLogSize)
            {
                var responseText = await ReadResponseBodyAsync(responseBody);
                if (context.Response.StatusCode is >= 400 and <= 499)
                {
                    await _requestLoggerService.WriteError(context,
                        new HttpRequestException($"""
                        Request: {context.Request.Method} {context.Request.Path}
                        Request Body: {requestBody}
                        Response: {responseText}
                        """), context.Response.StatusCode);
                }
            }
        }
        catch
        {
            // Exception 發生，不做任何 Response 操作，讓 ExceptionHandleMiddleware 處理
            throw;
        }
        finally
        {
            // 還原原始 Response Stream
            context.Response.Body = originalBodyStream;

            if (responseCopied)
            {
                responseBody.Position = 0;
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
    }

    /// <summary>
    /// 讀取request內容
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private static async Task<string> ReadRequestBodyAsync(HttpContext context)
    {
        if (context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            return body;
        }

        if (!context.Request.HasFormContentType) return string.Empty;
        var form = await context.Request.ReadFormAsync();
        var dict = new Dictionary<string, object>
        {
            ["_form"] = form.ToDictionary(x => x.Key, x => x.Value.ToString())
        };

        if (form.Files.Any())
        {
            var files = form.Files.Select(f => new
            {
                f.Name,
                f.FileName,
                f.ContentType,
                f.Length,
                content = $"<File Content: {f.Length} bytes>"
            }).ToList();
            dict["_files"] = files;
        }

        context.Request.Body.Position = 0;
        return JsonConvert.SerializeObject(dict);

    }

    /// <summary>
    /// 讀取response Body內容
    /// </summary>
    /// <param name="responseBody"></param>
    /// <returns></returns>
    private static async Task<string> ReadResponseBodyAsync(MemoryStream responseBody)
    {
        responseBody.Position = 0;
        var text = await new StreamReader(responseBody).ReadToEndAsync();
        responseBody.Position = 0;
        return text;
    }
}