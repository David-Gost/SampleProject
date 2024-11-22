using System.ComponentModel;

namespace SampleProject.Base.Interface.RequestFrom;

/// <summary>
/// request需有排序時加入
/// </summary>
public interface IOrderList
{
    /// <summary>
    /// 排序值陣列，所見值參數排序 ，範例：以更新時間逆排序"orderList":["updatedAt|DESC"]"
    /// </summary>
    [DefaultValue(null)]
    public List<string>? orderList { get; set; }
}