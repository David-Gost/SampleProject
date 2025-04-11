namespace Base.Models.File;

/// <summary>
/// 檔案上傳回應資訊
/// </summary>
public class FileInfoModel
{
    /// <summary>
    /// 原始檔案名稱
    /// </summary>
    public string originalFileName { get; set; }
    
    /// <summary>
    /// 上傳serv檔案名稱
    /// </summary>
    public string fileName { get; set; }
    
    /// <summary>
    /// 檔案上傳路徑
    /// </summary>
    public string filePath { get; set; }
    
    /// <summary>
    /// mineType
    /// </summary>
    public string mimeType { get; set; }

    /// <summary>
    /// 副檔名
    /// </summary>
    public string extension { get; set; }

    /// <summary>
    /// 檔案連結
    /// </summary>
    public string fileUrl { get; set; }
}