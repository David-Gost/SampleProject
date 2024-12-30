namespace SampleProject.Base.Interface.DB.PEC;

/// <summary>
/// PEC專案用紀錄更新時間,新增時間欄位
/// </summary>
public interface ITimestampable
{
    public DateTime? creationDate { get; set; }
    /// <summary>
    /// 新增人員（user id）
    /// </summary>
    public int createdBy { get; set; }
    
    public DateTime? lastModifyDate { get; set; }
    /// <summary>
    /// 修改人員（user id）
    /// </summary>
    public int lastModifyBy { get; set; }
}