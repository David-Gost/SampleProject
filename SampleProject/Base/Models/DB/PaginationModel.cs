namespace SampleProject.Base.Models.DB;

/// <summary>
/// 產生帶有分頁的Model
/// </summary>
public class PaginationModel<T>
{
    /// <summary>
    /// 資料總筆數
    /// </summary>
    public int dataCount { get; set; } = 0;
    
    /// <summary>
    /// 總頁數
    /// </summary>
    public int totalPages { get; set; } = 0;
    
    /// <summary>
    /// 當前頁數
    /// </summary>
    public int pageNumber { get; set; } = 1;
    
    /// <summary>
    /// 每頁資料筆數
    /// </summary>
    public int pageSize { get; set; } = 15;
    
    /// <summary>
    /// 資料
    /// </summary>
    public IEnumerable<T> data { get; set; } = new List<T>();
}

