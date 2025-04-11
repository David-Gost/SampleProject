using Base.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Base.Models.File;
using Base.Models.Http;
using Base.Models.Http.AuthType;
using Base.Models.Http.Form;
using SampleProject.Helpers;
using SampleProject.Services.Custom;

namespace SampleProject.Controllers;

public class TestController : BaseApiController
{
    private readonly CrontabTasksService _crontabTasksService;

    public TestController(CrontabTasksService crontabTasksService)
    {
        _crontabTasksService = crontabTasksService;
    }

    /// <summary>
    /// 檔案上傳
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    [HttpPost("UploadFile")]
    public Task<IActionResult> UploadFile(IFormFile? file)
    {
        var request = HttpContext.Request;
        var domainName = request.Host.Value;
        var scheme = request.Scheme;

        var fileOption = new UploadOptionModel
        {
            uploadPath = "",
            baseUrl = $"{scheme}://{domainName}",
            allowAllExtension = true,
        };
        return Task.FromResult(BackCall(FileHelper.UploadDocument(file, fileOption)!));
    }

    /// <summary>
    /// 移動檔案
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    [HttpPost("MoveFile")]
    public Task<IActionResult> MoveFile(string filePath)
    {
        var request = HttpContext.Request;
        var domainName = request.Host.Value;
        var scheme = request.Scheme;
        var baseUrl = $"{scheme}://{domainName}";

        return Task.FromResult(BackCall(FileHelper.MoveFile(filePath, "moveTo", baseUrl)!));
    }

    /// <summary>
    /// 打API測試
    /// </summary>
    /// <returns></returns>
    [HttpGet("CallApiTest")]
    public Task<IActionResult> CallApiTest()
    {
        var jwtAuthModel = new JwtAuthModel
        {
            token =
                "eyJhbGciOiJIUzI1NiJ9.W9mSdC0zsW4NlA8T6fvaE4uz4hgYcpVBSRv5T_lbMBapbZKChbvBemBxvtk4doUfv2ErleamidX7Ojz3cLiBvTxfRt7unA8y6UDeqJuboBx6YRgZU6zkGshiYa8s9fIb74RMtQcmVPfLor70w6irfqILKr8sFezo0lJCaxw8cUo.JvqoInwQdYYkuvP1TMtFgVIa32bbrgzWDzLK9yfPdL8"
        };
        var clientOption = new ClientOptionModel
        {
            requestApiUrl = "http://localhost:5209/FormApiTest",
            httpMethod = HttpMethod.Post,
            authModel = jwtAuthModel,
            headerParams = new Dictionary<string, string> { { "a", "a_val" } }
        };

        var fromFileFullPath1 = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
            "moveTo/0B04474E3219ACD0F29E5DE0EEA2B997_2I4FIIpSP.jpg");
        var fromFileFullPath2 = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
            "moveTo/b627038af204d02e202039ea8be93d9f29b68b4b78110096df113054e264664f.doc");

        var formDataList = new List<FormContentModel>
        {
            {
                new FormContentModel
                {
                    dataKey = "k1",
                    dataVal = "val1"
                }
            },
            new FormContentModel
            {
                dataType = FormContentModel.DATA_TYPE_FILE,
                dataKey = "K2",
                dataVal = fromFileFullPath1
            },
            new FormContentModel
            {
                dataType = FormContentModel.DATA_TYPE_FILE,
                dataKey = "K3",
                dataVal = fromFileFullPath2
            }
        };

        return Task.FromResult(BackCall(HttpHelper.FormRequest(formDataList, clientOption).Result!));
    }

    /// <summary>
    /// 表單API測試
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    [HttpPost("FormApiTest")]
    public Task<IActionResult> FormApiTest([FromForm] IFormCollection data)
    {
        return Task.FromResult(BackCall(data));
    }
    
    [HttpPost("DbTest")]
    public Task<IActionResult> DbTest()
    {
        var resultData = _crontabTasksService.GetAllData();
        return Task.FromResult(BackCall(resultData));
    }
}