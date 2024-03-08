using System.Security.Cryptography;
using SampleProject.Base.Models.File;

namespace SampleProject.Helpers;

public static class FileHelper
{
    private static readonly string _basePath = "wwwroot";

    /// <summary>
    /// 基本上傳檔案，成功回傳檔案資訊
    /// </summary>
    /// <param name="fromFile"></param>
    /// <param name="uploadOption"></param>
    /// <returns></returns>
    public static FileInfoModel? BaseUploadFile(IFormFile fromFile, UploadOptionModel uploadOption)
    {
        var pathName = uploadOption.uploadPath ?? "";
        var baseUrl = uploadOption.baseUrl;

        var checkUploadFile = CheckFile(fromFile, uploadOption.allowExtension, uploadOption.allowMimeType,
            uploadOption.allowFileSize);

        //檢查檔案是否可上傳
        if (!checkUploadFile.checkStatus)
        {
            return null;
        }

        //檢查網址句尾是否有/，沒有就補上
        if (!baseUrl.Equals("") && !baseUrl.EndsWith($"/"))
        {
            baseUrl += "/";
        }

        //檔案上傳位置
        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), $"{_basePath}/{pathName}");

        //檢查路徑是否存在，不存在則建立
        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        try
        {
            using var memoryStream = new MemoryStream();
            fromFile.CopyToAsync(memoryStream);

            //依照檔案內容生成md5
            var hash = MD5.HashData(memoryStream.ToArray());
            var hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            var randomString = DataHelper.RandomString(9);

            //新檔名
            var newFileName = $"{hashString}_{randomString}" + Path.GetExtension(fromFile.FileName);

            //檔案完整路徑
            var fileFullPath = Path.Combine(uploadPath, newFileName);

            using var stream = new FileStream(fileFullPath, FileMode.Create);
            // 寫入檔案到指定路徑
            fromFile.CopyToAsync(stream);

            if (!File.Exists(fileFullPath))
            {
                return null;
            }

            //帶入檔案資訊
            var fileInfoModel = new FileInfoModel
            {
                originalFileName = fromFile.FileName,
                fileName = newFileName,
                filePath = Path.Combine(pathName,newFileName),
                mimeType = fromFile.ContentType,
                extension = Path.GetExtension(fromFile.FileName),
                fileUrl = !baseUrl.Equals("") ? $"{baseUrl}{pathName}/{newFileName}" : ""
            };

            return fileInfoModel;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }
    
    /// <summary>
    /// 檢查檔案是否可上傳
    /// </summary>
    /// <param name="uploadFile"></param>
    /// <param name="allowExtension"></param>
    /// <param name="allowMimeType"></param>
    /// <param name="allowFileSize"></param>
    /// <returns></returns>
    private static CheckUploadFileModel CheckFile(
        IFormFile? uploadFile,
        List<string> allowExtension = null,
        List<string> allowMimeType = null,
        int allowFileSize = 0)
    {
        var checkUploadFileModel = new CheckUploadFileModel
        {
            checkStatus = false
        };

        var fileSize = uploadFile?.Length ?? 0;
        //判斷是否有檔案
        if (uploadFile == null || fileSize == 0)
        {
            checkUploadFileModel.statusCode = CheckUploadFileModel.STATUS_FILE_IS_NULL;

            return checkUploadFileModel;
        }


        //檢查檔案類型是否允許上傳
        if (allowExtension is { Count: > 0 })
        {
            var fileExtension = Path.GetExtension(uploadFile.FileName).ToLower();
            if (!allowExtension.Contains(fileExtension))
            {
                checkUploadFileModel.statusCode = CheckUploadFileModel.STATUS_FILE_TYPE_NOT_ALLOW;
            }
        }

        //檢查檔案MIME是否允許上傳
        if (allowMimeType is { Count: > 0 })
        {
            var fileMimeType = uploadFile.ContentType.ToLower();
            if (!allowMimeType.Contains(fileMimeType))
            {
                checkUploadFileModel.statusCode = CheckUploadFileModel.STATUS_FILE_MIME_NOT_ALLOW;
            }
        }

        //檢查檔案大小是否超過限制
        if (allowFileSize > 0 && fileSize > allowFileSize)
        {
            checkUploadFileModel.statusCode = CheckUploadFileModel.STATUS_FILE_OVER_SIZE;
        }

        if (checkUploadFileModel.statusCode == CheckUploadFileModel.STATUS_NONE)
        {
            //檢查通過，設定狀態為上傳成功
            checkUploadFileModel.checkStatus = true;
            checkUploadFileModel.statusCode = CheckUploadFileModel.STATUS_OK;
        }

        return checkUploadFileModel;
    }
}