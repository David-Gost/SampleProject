using System.Dynamic;
using Microsoft.AspNetCore.Mvc;

namespace SampleProject.Controllers.Base;

[ApiController]
public class BaseApiController : ControllerBase
{
    /// <summary>
    /// 定義API回傳資料
    /// </summary>
    /// <param name="resultData">資料內容</param>
    /// <param name="httpCode">httpCode</param>
    /// <param name="message">訊息</param>
    /// <param name="dataCode">資料代碼，依照狀況使用，預設可不填寫</param>
    /// 
    /// <returns></returns>
    protected ActionResult BackCall(
        object resultData,
        int httpCode = 200,
        string message = "",
        string dataCode = "")
    {
        //定義回傳資料物件
        dynamic responseBodyData = new ExpandoObject();

        responseBodyData.HttpCode = httpCode;

        //dataCode不為空時多回應
        if (!dataCode.Equals(""))
        {
            responseBodyData.DataCode = dataCode;
        }

        //無傳回應資料時產生空物件
        resultData ??= new ExpandoObject();

        responseBodyData.Data = resultData;

        responseBodyData.Message = message;

        return httpCode == 204 ? NoContent() : (ActionResult)StatusCode(httpCode, responseBodyData);
    }
}