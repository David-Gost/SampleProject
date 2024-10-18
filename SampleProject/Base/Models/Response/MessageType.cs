namespace SampleProject.Base.Models.Response;

public class MessageType
{
    /// <summary>
    /// 請求完成
    /// </summary>
    public const string SUCCESS = "request complete";
    
    /// <summary>
    /// 請求錯誤（資料檢查異常 or 傳送資料有誤）
    /// </summary>
    public const string REQUEST_ERROR = "request error";
    
    /// <summary>
    /// 系統錯誤（系統出現例外錯誤）
    /// </summary>
    public const string SYSTEM_ERROR = "system error";
}