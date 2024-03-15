namespace SampleProject.Base.Models.Http;

public class ClientOptionModel
{
    /// <summary>
    /// API URL
    /// </summary>
    public string requestApiUrl { get; set; }

    /// <summary>
    /// HttpMethod
    /// </summary>
    public HttpMethod httpMethod { get; set; }

    /// <summary>
    /// 自訂Header
    /// </summary>
    public Dictionary<string, string> headerParams { get; set; }

    /// <summary>
    /// url params參數
    /// </summary>
    public Dictionary<string, string> urlParams { get; set; }

    /// <summary>
    /// BearerToken 數值
    /// </summary>
    public string bearerToken { get; set; } = "";
}