namespace Base.Interface.DB.PEC;

/// <summary>
/// PEC專案用軟刪除，是否顯示資料的判斷依據為
/// </summary>
public interface ISoftDeletable
{
    public DateTime? deletionDate { get; set; }
    
    /// <summary>
    /// 刪除人員（user id）
    /// </summary>
    public int deletedBy { get; set; }
}