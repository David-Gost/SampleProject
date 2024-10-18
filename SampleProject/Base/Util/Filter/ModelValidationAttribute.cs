using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using Dapper;
using ElmahCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SampleProject.Base.Models.Response;

namespace SampleProject.Base.Util.Filter;

public class ModelValidationAttribute : ActionFilterAttribute
{
    private readonly ErrorLog _errorLog;

    public ModelValidationAttribute(ErrorLog errorLog)
    {
        _errorLog = errorLog;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.Result != null || context.ModelState.IsValid)
        {
            return;
        }

        //取出欄位錯誤訊息
        var errorMessages = context.ModelState.ToDictionary(
            p => p.Key,
            p => p.Value!.Errors.Select(e => e.ErrorMessage));

        var resultBody = new BaseApiResponse()
        {
            messageType = MessageType.REQUEST_ERROR
        };

        var checkHaveInputData = errorMessages.ContainsKey("inputData");
        var exceptionMessage = "";
        if (checkHaveInputData)
        {
            var messageList = errorMessages.SelectMany(x => x.Value).AsList();
            resultBody.message = messageList;
            exceptionMessage = string.Join(Environment.NewLine, messageList);
        }
        else
        {
            resultBody.message = errorMessages;
            exceptionMessage = string.Join(Environment.NewLine + "  ",
                errorMessages.Select(x => x.Key + "：" + string.Join(Environment.NewLine, x.Value)));
        }

        const int statusCode = (int)HttpStatusCode.BadRequest;
        //紀錄log
        var httpContext = context.HttpContext;
        var requestBody = httpContext.Items["requestBody"]?.ToString()!;

        _errorLog.LogAsync(new Error(
            new HttpRequestException("requestError", new HttpRequestException(exceptionMessage))
            , httpContext)
        {
            StatusCode = statusCode
        });

        //改寫輸出
        context.Result = new ObjectResult(resultBody)
        {
            StatusCode = statusCode
        };
    }
}