namespace Base.Interface.RequestFrom;

/// <summary>
/// 分頁查詢
/// </summary>
public interface IPagination
{
    /// <summary>
    /// 每頁筆數
    /// </summary>
    public int? pageSize { get; set; }
    
    /// <summary>
    /// 需查詢頁碼
    /// </summary>
    public int? pageNumber { get; set; }
}