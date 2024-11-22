namespace SampleProject.Base.Interface.DB;

/// <summary>
/// 軟刪除，是否顯示資料的判斷依據為
/// </summary>
public interface ISoftDeletable
{
    public DateTime? deletedAt { get; set; }
}