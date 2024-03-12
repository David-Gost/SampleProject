namespace SampleProject.Base.Models.File;

/// <summary>
/// 檢查檔案上傳處理
/// </summary>
public class CheckUploadFileModel
{
    /// <summary>
    /// 無狀態
    /// </summary>
    public const int STATUS_NONE = 0;
    
    /// <summary>
    /// 上傳成功
    /// </summary>
    public const int STATUS_OK = 1;
    
    /// <summary>
    /// 檔案類型不允許上傳
    /// </summary>
    public const int STATUS_FILE_EXTENSION_NOT_ALLOW = 2;
    
    /// <summary>
    /// 檔案mime不允許上傳
    /// </summary>
    public const int STATUS_FILE_MIME_NOT_ALLOW = 3;
    
    /// <summary>
    /// 文件過大
    /// </summary>
    public const int STATUS_FILE_OVER_SIZE = 4;
    
    /// <summary>
    /// 無檔案資料
    /// </summary>
    public const int STATUS_FILE_IS_NULL = 5;

    /// <summary>
    /// 狀態檢查結果
    /// </summary>
    public bool checkStatus { get; set; } = false;

    /// <summary>
    /// 上傳結果代號
    /// </summary>
    public int statusCode { get; set; } = STATUS_NONE;
}