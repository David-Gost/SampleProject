using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Base.Models.Response;
using SampleProject.Services.Custom.Log;

namespace SampleProject.Util.Filter;

public class ModelValidationAttribute : ActionFilterAttribute
{
    private readonly RequestLoggerService _requestLoggerService;

    public ModelValidationAttribute(RequestLoggerService requestLoggerService)
    {
        _requestLoggerService = requestLoggerService;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
        {
            base.OnActionExecuting(context);
            return;
        }

        try
        {
            //取出欄位錯誤訊息
            var errorMessages = context.ModelState
                .Where(p => p.Value != null && p.Value.Errors.Any())
                .ToDictionary(
                    p => p.Key,
                    p => p.Value!.Errors.Select(e => e.ErrorMessage).ToList());

            var resultBody = new BaseApiResponse()
            {
                messageType = MessageType.REQUEST_ERROR
            };

            var checkHaveInputData = errorMessages.ContainsKey("inputData");
            if (checkHaveInputData)
            {
                var messageList = errorMessages.SelectMany(x => x.Value).ToList();
                resultBody.message = messageList;
            }
            else
            {
                resultBody.message = errorMessages;
            }

            const int statusCode = (int)HttpStatusCode.BadRequest;
            
            // 改寫輸出
            context.Result = new ObjectResult(resultBody)
            {
                StatusCode = statusCode
            };
        }
        catch (Exception e)
        {
            e.Source = "ModelValidationAttribute";
            // 確保即使發生例外，也執行基底方法
            base.OnActionExecuting(context);
        }
    }
}