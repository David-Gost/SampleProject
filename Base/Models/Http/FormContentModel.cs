namespace Base.Models.Http.Form;

public class FormContentModel
{
    private const int DATA_TYPE_BASE = 0;
    public const int DATA_TYPE_FILE = 1;

    /// <summary>
    /// 資料類型，為FILE時會讀取dataVal中資料
    /// </summary>
    public int dataType { get; set; } = DATA_TYPE_BASE;

    /// <summary>
    /// Key值，如為陣列時請輸入如下 keyName[index]
    /// </summary>
    public string dataKey { get; set; }

    /// <summary>
    /// 數值，需要上傳檔案時請帶入檔案完整路徑，且dataType需為DATA_TYPE_FILE
    /// </summary>
    public object dataVal { get; set; }
}