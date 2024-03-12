using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.StaticFiles;
using SampleProject.Base.Models.File;

namespace SampleProject.Helpers;

public static class FileHelper
{
    private const string BASE_PATH = "wwwroot";

    /// <summary>
    /// 移動檔案
    /// </summary>
    /// <param name="fromFilePath">不包含跟目錄的相對路徑</param>
    /// <param name="toPathName">移動目標位置</param>
    /// <param name="baseUrl">網站baseUrl</param>
    /// <returns></returns>
    public static FileInfoModel? MoveFile(string fromFilePath="", string toPathName="",string baseUrl="")
    {

        var fromFileFullPath = Path.Combine(Directory.GetCurrentDirectory(),BASE_PATH, fromFilePath);

        if (fromFilePath.Equals("") || toPathName.Equals(""))
        {

            return null;
        }
        
        //檢查檔案是否存在
        if (File.Exists(fromFileFullPath))
        {
            
            //檢查網址句尾是否有/，沒有就補上
            if (!baseUrl.Equals("") && !baseUrl.EndsWith($"/"))
            {
                baseUrl += "/";
            }

            //檔案上傳位置
            var newFilePath = $"{BASE_PATH}/{toPathName}";
            var newFullPath = Path.Combine(Directory.GetCurrentDirectory(), newFilePath);

            //檢查路徑是否存在，不存在則建立
            if (!Directory.Exists(newFullPath))
            {
                Directory.CreateDirectory(newFullPath);
            }
            
            var fileHash = GenerateFileMD5(fromFileFullPath);
            
            var randomString = DataHelper.RandomString(9);
            
            //檔案副檔名
            var extension = Path.GetExtension(fromFileFullPath);

            //新檔名
            var newFileName = $"{fileHash}_{randomString}{extension}";

            //檔案完整路徑
            var newFileFullPath = Path.Combine(newFullPath, newFileName);
            
            var provider = new FileExtensionContentTypeProvider();

            // Try to get the MIME type of the file
            if (!provider.TryGetContentType(fromFileFullPath, out var contentType))
            {
                contentType = "application/octet-stream"; // 默认 MIME 类型
            }

            //獨動檔案
            File.Move(fromFileFullPath,newFileFullPath);
 
            if (!File.Exists(newFileFullPath))
            {
                return null;
            }

            //帶入檔案資訊
            var fileInfoModel = new FileInfoModel
            {
                originalFileName = "",
                fileName = newFileName,
                filePath = newFilePath,
                mimeType = contentType,
                extension = extension,
                fileUrl = !baseUrl.Equals("") ? $"{baseUrl}{toPathName}/{newFileName}" : ""
            };

            return fileInfoModel;
        }
        else
        {
            return null;
        }
    }
    
    /// <summary>
    /// 文件檔上傳
    /// </summary>
    /// <param name="formFileData">上傳來源</param>
    /// <param name="uploadOption">檔案上傳選項</param>
    /// <typeparam name="T">允許List&lt;IFormFile&gt;()或 IFormFile</typeparam>
    /// <returns></returns>
    public static UploadResultModel UploadDocument<T>(T? formFileData, UploadOptionModel uploadOption = null!)
    {
        uploadOption ??= new UploadOptionModel();

        //預設上傳到網站根目錄/images裡面
        if (uploadOption is null or { uploadPath: "" })
        {
            uploadOption!.uploadPath = "documents";
        }

        //允許副檔名
        if (uploadOption.allowExtension is { Count: 0 })
        {
            uploadOption.allowExtension = [".txt", ".json", ".pdf", ".doc",
                ".xls", ".ppt", ".docx", ".xlsx", ".pptx"];
        }

        //允許mime
        if (uploadOption.allowMimeType is { Count: 0 })
        {
            uploadOption.allowMimeType =
            [
                "text/plain", "application/json", "application/pdf", "application/msword", "application/vnd.ms-excel", "application/vnd.ms-powerpoint",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "application/vnd.openxmlformats-officedocument.presentationml.presentation"
            ];
        }
        
        return formFileData switch
        {
            List<IFormFile> fileList =>
                //多檔上傳
                UploadMultiFile(fileList!, uploadOption),
            IFormFile formFile =>
                //單檔上傳
                UploadSingleFile(formFile, uploadOption),
            _ => UploadSingleFile(null, uploadOption)
        };
    }

    /// <summary>
    /// 圖片上傳
    /// </summary>
    /// <param name="formFileData">上傳來源</param>
    /// <param name="uploadOption">檔案上傳選項</param>
    /// <typeparam name="T">允許List&lt;IFormFile&gt;()或 IFormFile</typeparam>
    /// <returns></returns>
    public static UploadResultModel UploadImageFile<T>(T? formFileData, UploadOptionModel uploadOption = null!)
    {
        uploadOption ??= new UploadOptionModel();

        //預設上傳到網站根目錄/images裡面
        if (uploadOption is null or { uploadPath: "" })
        {
            uploadOption!.uploadPath = "images";
        }

        //允許副檔名
        if (uploadOption.allowExtension is { Count: 0 })
        {
            uploadOption.allowExtension = [".jpeg", ".jpg", ".png", ".bmp", ".svg", ".webp", ".ico"];
        }

        //允許mime
        if (uploadOption.allowMimeType is { Count: 0 })
        {
            uploadOption.allowMimeType =
            [
                "image/jpeg", "image/png", "image/bmp", "image/svg+xml", "image/webp", "image/vnd.microsoft.icon",
                "image/x-ico"
            ];
        }

        return formFileData switch
        {
            List<IFormFile> fileList =>
                //多檔上傳
                UploadMultiFile(fileList!, uploadOption),
            IFormFile formFile =>
                //單檔上傳
                UploadSingleFile(formFile, uploadOption),
            _ => UploadSingleFile(null, uploadOption)
        };
    }

    /// <summary>
    /// 多筆上傳檔案
    /// </summary>
    /// <param name="formFileList"></param>
    /// <param name="uploadOption"></param>
    /// <returns></returns>
    public static UploadResultModel UploadMultiFile(List<IFormFile?> formFileList, UploadOptionModel uploadOption)
    {
        var uploadResult = new UploadResultModel
        {
            message = "",
            fileData = null
        };

        if (formFileList is not { Count: not 0 })
        {
            uploadResult.statusCode = CheckUploadFileModel.STATUS_FILE_IS_NULL;
            uploadResult.message = StatusCodeToMessage(uploadResult.statusCode);

            return uploadResult;
        }

        var checkStatus = true;
        var message = "";

        //檢查檔案清單
        for (var index = 0; index < formFileList.Count; index++)
        {
            var formFile = formFileList[index];
            var checkUploadFile = CheckFile(formFile, uploadOption.allowExtension, uploadOption.allowMimeType,
                uploadOption.allowFileSize);

            if (checkStatus && !checkUploadFile.checkStatus)
            {
                checkStatus = false;
            }

            if (!checkUploadFile.checkStatus)
            {
                message += $"index of {index} file {StatusCodeToMessage(checkUploadFile.statusCode)} <br>";
            }
        }

        if (!checkStatus)
        {
            uploadResult.message = message;
            return uploadResult;
        }

        //執行上傳
        uploadResult.statusCode = CheckUploadFileModel.STATUS_OK;
        uploadResult.fileData = formFileList.Select(formFile => BaseUploadFile(formFile, uploadOption)).ToList();
        uploadResult.message = StatusCodeToMessage(uploadResult.statusCode);

        return uploadResult;
    }

    /// <summary>
    /// 單檔上傳
    /// </summary>
    /// <param name="formFile"></param>
    /// <param name="uploadOption"></param>
    /// <returns></returns>
    public static UploadResultModel UploadSingleFile(IFormFile? formFile, UploadOptionModel uploadOption)
    {
        var checkUploadFile = CheckFile(formFile, uploadOption.allowExtension, uploadOption.allowMimeType,
            uploadOption.allowFileSize);

        var uploadResult = new UploadResultModel
        {
            message = "",
            fileData = new FileInfoModel()
        };

        if (checkUploadFile.checkStatus)
        {
            //上傳檔案
            var fileInfo = BaseUploadFile(formFile, uploadOption);

            if (fileInfo != null)
            {
                uploadResult.statusCode = CheckUploadFileModel.STATUS_OK;
                uploadResult.message = StatusCodeToMessage(uploadResult.statusCode);
                uploadResult.fileData = fileInfo;
            }
            else
            {
                uploadResult.statusCode = CheckUploadFileModel.STATUS_FILE_IS_NULL;
            }
        }

        if (uploadResult.statusCode == CheckUploadFileModel.STATUS_OK)
        {
            return uploadResult;
        }

        uploadResult.statusCode = checkUploadFile.statusCode;
        uploadResult.message = StatusCodeToMessage(uploadResult.statusCode);
        uploadResult.fileData = null;

        return uploadResult;
    }

    /// <summary>
    /// 基本上傳檔案，成功回傳檔案資訊
    /// </summary>
    /// <param name="formFile"></param>
    /// <param name="uploadOption"></param>
    /// <returns></returns>
    public static FileInfoModel? BaseUploadFile(IFormFile? formFile, UploadOptionModel uploadOption)
    {
        var pathName = uploadOption.uploadPath ?? "";
        var baseUrl = uploadOption.baseUrl;

        var checkUploadFile = CheckFile(formFile, uploadOption.allowExtension, uploadOption.allowMimeType,
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
        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), $"{BASE_PATH}/{pathName}");

        //檢查路徑是否存在，不存在則建立
        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        try
        {
            
            // //依照檔案內容生成md5
            var md5 = MD5.Create();
            var stream = formFile!.OpenReadStream();
            var hash = md5.ComputeHash(stream);
            var hashString= BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            
            var randomString = DataHelper.RandomString(9);

            //新檔名
            var newFileName = $"{hashString}_{randomString}" + Path.GetExtension(formFile.FileName);

            //檔案完整路徑
            var fileFullPath = Path.Combine(uploadPath, newFileName);

            stream = new FileStream(fileFullPath, FileMode.Create);
            // 寫入檔案到指定路徑
            formFile.CopyToAsync(stream);

            if (!File.Exists(fileFullPath))
            {
                return null;
            }

            //帶入檔案資訊
            var fileInfoModel = new FileInfoModel
            {
                originalFileName = formFile.FileName,
                fileName = newFileName,
                filePath = Path.Combine(pathName, newFileName),
                mimeType = formFile.ContentType,
                extension = Path.GetExtension(formFile.FileName),
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
            for (var index = 0; index < allowExtension.Count; index++)
            {
                var extensionName = allowExtension[index];
                if (!extensionName.StartsWith($"."))
                {
                    allowExtension[index] = $".{extensionName}";
                }
            }

            var fileExtension = Path.GetExtension(uploadFile.FileName).ToLower();
            if (!allowExtension.Contains(fileExtension))
            {
                checkUploadFileModel.statusCode = CheckUploadFileModel.STATUS_FILE_EXTENSION_NOT_ALLOW;
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

    /// <summary>
    /// 狀態碼轉換為訊息
    /// </summary>
    /// <param name="statusCode"></param>
    /// <returns></returns>
    private static string StatusCodeToMessage(int statusCode = CheckUploadFileModel.STATUS_NONE)
    {
        var message = statusCode switch
        {
            CheckUploadFileModel.STATUS_OK => "upload successful",
            CheckUploadFileModel.STATUS_FILE_OVER_SIZE => "is too large",
            CheckUploadFileModel.STATUS_FILE_EXTENSION_NOT_ALLOW => "not allowed",
            CheckUploadFileModel.STATUS_FILE_MIME_NOT_ALLOW => "not allowed",
            CheckUploadFileModel.STATUS_FILE_IS_NULL => "no file data",
            _ => ""
        };

        return message;
    }

    /// <summary>
    /// 產生文件Hash
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    private static string GenerateFileMD5(string filename)
    {
        
        using var md5 = MD5.Create();
        // 讀檔
        using var stream = File.OpenRead(filename);
        // 產生文件hash
        var hash = md5.ComputeHash(stream);
            
        //Hash轉換為stringBuilder
        var stringBuilder = new StringBuilder(hash.Length * 2);

        foreach (var byteVal in hash)
        {
            stringBuilder.Append(byteVal.ToString("X2"));
        }

        //返回檔案Hash
        return stringBuilder.ToString();
    }
}