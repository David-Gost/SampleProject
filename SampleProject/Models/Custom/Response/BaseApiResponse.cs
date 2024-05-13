namespace SampleProject.Models.Custom.Response;

/// <summary>
/// 基本API回應格式
/// </summary>
public class BaseApiResponse
{
    /// <summary>
    /// 資料代碼
    /// </summary>
    public string dataCode { get; set; }
    
    /// <summary>
    /// 訊息類型
    /// </summary>
    public object messageType { get; set; }
    
    /// <summary>
    /// 回應訊息
    /// </summary>
    public object message { get; set; }
    
    /// <summary>
    /// 資料內容
    /// </summary>
    public object result { get; set; }
    
}