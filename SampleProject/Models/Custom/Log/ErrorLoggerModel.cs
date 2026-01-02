namespace SampleProject.Models.Custom.Log;

public class ErrorLoggerModel
{
    public Exception? exception { get; }
    public HttpContext httpContext { get; }
    public string? requestBody { get; }
    public int statusCode { get; set; }

    public ErrorLoggerModel(Exception? ex, HttpContext context, string? requestBody)
    {
        exception = ex;
        httpContext = context;
        this.requestBody = requestBody;
    }
    public ErrorLoggerModel(Exception? ex, HttpContext context, string? requestBody, int statusCode)
    {
        exception = ex;
        httpContext = context;
        this.requestBody = requestBody;
        this.statusCode = statusCode;
        
    }
}