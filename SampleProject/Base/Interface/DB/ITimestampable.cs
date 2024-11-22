namespace SampleProject.Base.Interface.DB;

/// <summary>
/// 紀錄更新時間,新增時間欄位
/// </summary>
public interface ITimestampable
{
    public DateTime? createdAt { get; set; }
    public DateTime? updatedAt { get; set; }
}