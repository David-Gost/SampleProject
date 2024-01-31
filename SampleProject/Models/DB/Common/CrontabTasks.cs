using System.ComponentModel.DataAnnotations.Schema;
namespace SampleProject.Models.DB;


[Table("crontab_tasks")] 
public class CrontabTasks 
{
  
  public int id { get; set;}

  /// <summary>
  /// 對應class名稱
  /// </summary>
  public string className { get; set;}

  /// <summary>
  /// 對應method名稱
  /// </summary>
  public string methodName { get; set;}

  /// <summary>
  /// method用參數
  /// </summary>
  public string argv { get; set;}

  /// <summary>
  /// minute hour day_of_month month weekday
  /// </summary>
  public string cronExpression { get; set;}

  /// <summary>
  /// 開始執行時間
  /// </summary>
  public int startTime { get; set;}

  /// <summary>
  /// 結束執行時間
  /// </summary>
  public int endTime { get; set;}

  /// <summary>
  /// 啟用狀態 0:未啟用，1:啟用
  /// </summary>
  public int status { get; set;}

  /// <summary>
  /// 執行狀態 0:未執行，1:已執行
  /// </summary>
  public int runningStatus { get; set;}

  /// <summary>
  /// 其他回傳資料
  /// </summary>
  public string callBackMessage { get; set;}

  /// <summary>
  /// 其他說明
  /// </summary>
  public string remark { get; set;}

  
  public DateTime created_at { get; set;}

  
  public DateTime updated_at { get; set;}

}
