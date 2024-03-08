namespace SampleProject.Base.Models.File;

/// <summary>
/// 檔案上傳
/// </summary>
public class UploadOptionModel
{
    /// <summary>
    /// 站台基本網址，有給值時上傳檔案後會輸出檔案網址
    /// </summary>
    public string baseUrl { get; set; } = "";

    /// <summary>
    /// 檔案上傳路徑
    /// </summary>
    public string uploadPath { get; set; }

    /// <summary>
    /// 允許副檔名
    /// </summary>
    public List<string> allowExtension { get; set; } = [];

    /// <summary>
    /// 允許檔案類型
    /// </summary>
    public List<string> allowMimeType { get; set; } = [];

    /// <summary>
    /// 允許上傳檔案大小，單位為Bytes，預設0代表不檢查
    /// </summary>
    public int allowFileSize { get; set; } = 0;
}