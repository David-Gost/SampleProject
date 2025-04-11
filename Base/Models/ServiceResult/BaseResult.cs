namespace Base.Models.ServiceResult;

public class BaseResult
{
    /// <summary>
    /// 訊息
    /// </summary>
    public object messages { get; set; } 
    
    /// <summary>
    /// 
    /// </summary>
    public object? resultData { get; set; } = null;
    
    /// <summary>
    /// 狀態碼，依照實際使用自行地義
    /// </summary>
    public int statusCode { get; set; } = 200;
}