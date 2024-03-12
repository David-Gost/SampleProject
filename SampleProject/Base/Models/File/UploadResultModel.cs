namespace SampleProject.Base.Models.File;

/// <summary>
/// 上傳導案回應資料
/// </summary>
public class UploadResultModel
{
    /// <summary>
    /// 狀態碼
    /// </summary>
    public int statusCode { get; set; } = CheckUploadFileModel.STATUS_NONE;

    /// <summary>
    /// 回應訊息
    /// </summary>
    public object? message { get; set; } = null;

    /// <summary>
    /// 檔案資料
    /// </summary>
    public object? fileData { get; set; } = null;
}