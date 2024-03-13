using Microsoft.AspNetCore.Mvc;
using SampleProject.Base.Controllers;
using SampleProject.Base.Models.File;
using SampleProject.Helpers;

namespace SampleProject.Controllers;

public class TestController:BaseApiController
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
}