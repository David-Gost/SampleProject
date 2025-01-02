using System.Net;

namespace SampleProject.Base.Models.Http;

public class ResponseModel
{
    /// <summary>
    /// http code
    /// </summary>
    public HttpStatusCode statusCode { get; set; }
    
    /// <summary>
    /// 回應內容
    /// </summary>
    public string content { get; set; } = "";

    /// <summary>
    /// content格式資料
    /// </summary>
    public ResponseContentType responseContentType { get; set; } 
}