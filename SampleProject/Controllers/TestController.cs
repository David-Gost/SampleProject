using Microsoft.AspNetCore.Mvc;
using SampleProject.Base.Controllers;
using SampleProject.Base.Models.File;
using SampleProject.Base.Models.Http;
using SampleProject.Helpers;

namespace SampleProject.Controllers;

public class TestController : BaseApiController
{
    /// <summary>
    /// 檔案上傳
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    [HttpPost("UploadFile")]
    public Task<ActionResult> UploadFile(IFormFile? file)
    {
        var request = HttpContext.Request;
        var domainName = request.Host.Value;
        var scheme = request.Scheme;

        var fileOption = new UploadOptionModel
        {
            uploadPath = "",
            baseUrl = $"{scheme}://{domainName}",
        };
        return Task.FromResult(BackCall(FileHelper.UploadDocument(file, fileOption)!));
    }

    /// <summary>
    /// 移動檔案
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    [HttpPost("MoveFile")]
    public Task<ActionResult> MoveFile(string filePath)
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
    public Task<ActionResult> CallApiTest()
    {
        var clientOption = new ClientOptionModel
        {
            requestApiUrl = "http://localhost:5100/api/Company/GetCompanyDatas",
            httpMethod = HttpMethod.Post,
            bearerToken =
                "eyJhbGciOiJIUzI1NiJ9.W9mSdC0zsW4NlA8T6fvaE4uz4hgYcpVBSRv5T_lbMBapbZKChbvBemBxvtk4doUfv2ErleamidX7Ojz3cLiBvTxfRt7unA8y6UDeqJuboBx6YRgZU6zkGshiYa8s9fIb74RMtQcmVPfLor70w6irfqILKr8sFezo0lJCaxw8cUo.JvqoInwQdYYkuvP1TMtFgVIa32bbrgzWDzLK9yfPdL8",
            headerParams = new Dictionary<string, string> { { "a", "a_val" } }
        };
        var jsonVal = "{\"id\": 0, \"companyId\": 0, \"companyName\": \"\"}";

        return Task.FromResult(BackCall(HttpHelper.RawContentRequest(jsonVal, clientOption).Result!));
    }
}