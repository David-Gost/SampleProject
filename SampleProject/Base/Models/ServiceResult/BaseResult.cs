namespace SampleProject.Base.Models.ServiceResult;

public class BaseResult
{
    /// <summary>
    /// 訊息
    /// </summary>
    public List<object> messages { get; set; } = [];
    
    /// <summary>
    /// 
    /// </summary>
    public object? resultData { get; set; } = null;
}