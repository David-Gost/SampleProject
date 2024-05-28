using System.Configuration;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using ElmahCore;
using FluentValidation;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Oracle.ManagedDataAccess.Client;
using SampleProject.Models.Custom.Response;

namespace SampleProject.Middleware;

public class ExceptionHandleMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ErrorLog _errorLog;

    public ExceptionHandleMiddleware(RequestDelegate next, ErrorLog errorLog)
    {
        _next = next;
        _errorLog = errorLog;
    }

    /// <summary>
    ///     任務調用
    /// </summary>
    /// <param name="context">HTTP 的上下文</param>
    /// <returns></returns>
    public async Task Invoke(HttpContext context)
    {
        var statusCode = context.Response.StatusCode;
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task<Task> HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var message = exception.Message;
        var guidNum = string.Concat("系統", Guid.NewGuid().ToString("D"));
        
        var httpStatusCode = (int)HttpStatusCode.InternalServerError;

        if (exception is OracleException)
        {
            var errorNum = (int)exception.GetType().GetProperty("Number")?.GetValue(exception, null)!;
            message = Enumerable.Range(20000, 20999).Contains(errorNum)
                ? exception.Message[..exception.Message.IndexOf('\n')]
                : string.Concat(guidNum, "--請聯絡資訊人員");
        }
        else
        {
            message = string.Concat(guidNum, "--請聯絡資訊人員");
        }

        var result = new BaseApiResponse()
        {
            dataCode = "0", messageType = MessageType.SYSTEM_ERROR, message = message
        };

        if (exception.GetType() == typeof(ValidationException))
        {
            
            httpStatusCode=(int)HttpStatusCode.Unauthorized;
            var exceptionInnerException = exception.InnerException;
            result.message = exceptionInnerException?.Message ?? "";
        }
        else
        {
            httpStatusCode=(int)HttpStatusCode.InternalServerError;
        }

        context.Response.StatusCode = httpStatusCode;

        //紀錄log
 
        var requestBody = context.Items["requestBody"]?.ToString()!;
        var errorData = new Error(exception, context)
        {
            StatusCode = httpStatusCode
        };
        await _errorLog.LogAsync(errorData);

        //輸出資料
        return Task.FromResult(context.Response.WriteAsJsonAsync(result));
    }
}