namespace SampleProject.Middleware;

/// <summary>
/// 暫存request資料
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        context.Request.EnableBuffering();

        var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
        
        context.Items["requestBody"] = requestBody;

        context.Request.Body.Position = 0;
        await _next(context);
    }
}