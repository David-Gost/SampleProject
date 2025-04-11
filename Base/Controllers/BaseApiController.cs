using System.Dynamic;
using Microsoft.AspNetCore.Mvc;
using Base.Models.Response;

namespace Base.Controllers;

[ApiController]
public class BaseApiController : ControllerBase
{
    /// <summary>
    /// 定義API回傳資料
    /// </summary>
    /// <param name="resultData">資料內容</param>
    /// <param name="httpCode">httpCode</param>
    /// <param name="messageType"></param>
    /// <param name="message">訊息</param>
    /// 
    /// <returns></returns>
    protected IActionResult BackCall(
        object resultData,
        object? message = null,
        string messageType = MessageType.SUCCESS,
        int httpCode = 200
    )
    {
        //定義回傳資料物件
        dynamic apiResponse = new BaseApiResponse();

        //dataCode不為空時多回應
        // if (string.IsNullOrEmpty(dataCode))
        // {
        //     apiResponse.dataCode = dataCode;
        // }

        //無傳回應資料時產生空物件
        resultData ??= new ExpandoObject();

        apiResponse.result = resultData;

        apiResponse.messageType = messageType;
        apiResponse.message = message ?? "";

        return httpCode == 204 ? NoContent() : StatusCode(httpCode, apiResponse);
    }
}