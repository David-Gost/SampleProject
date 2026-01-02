using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.StaticFiles;
using Base.Models.File;

namespace SampleProject.Helpers;

/// <summary>
/// 檔案處理相關Helper
/// </summary>
public static class FileHelper
{
    private const string BasePath = "wwwroot";

    /// <summary>
    /// 清除目錄內X天前檔案，注意使用
    /// </summary>
    /// <param name="directoryName">路徑名</param>
    /// <param name="dayNum">x天前所建立檔案</param>
    public static void ClearDirectoryFile(string directoryName = "temp", int dayNum = 7)
    {
        var pathVal = Path.Combine(Directory.GetCurrentDirectory(), BasePath, directoryName);
        var directory = new DirectoryInfo(pathVal);
        if (!directory.Exists)
        {
            return;
        }

        //搜尋符合條件的檔案清單
        var files = directory.GetFiles().Where(file => file.LastWriteTime < DateTime.Now.AddDays(-dayNum)).ToList();
        foreach (var file in files)
        {
            file.Delete();
        }
    }

    /// <summary>
    /// 刪除檔案
    /// </summary>
    /// <param name="filePath"></param>
    public static void DeleteFile(string filePath = "")
    {
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), BasePath, filePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    /// <summary>
    /// 刪除多筆檔案
    /// </summary>
    /// <param name="filePaths"></param>
    public static void DeleteFiles(List<string>? filePaths = null)
    {
        if (filePaths == null) return;
        foreach (var filePath in filePaths)
        {
            DeleteFile(filePath);
        }
    }

    /// <summary>
    /// 驗證檔案是否存在
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static bool ValidateFileExits(string filePath = "")
    {
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), BasePath, filePath);
        return File.Exists(fullPath);
    }

    /// <summary>
    /// 移動檔案
    /// </summary>
    /// <param name="fromFilePath">不包含跟目錄的相對路徑</param>
    /// <param name="toPathName">移動目標位置</param>
    /// <param name="baseUrl">網站baseUrl</param>
    /// <returns></returns>
    public static FileInfoModel? MoveFile(string fromFilePath = "", string toPathName = "", string baseUrl = "")
    {
        var fromFileFullPath = Path.Combine(Directory.GetCurrentDirectory(), BasePath, fromFilePath);

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
            var newFilePath = $"{BasePath}/{toPathName}";
            var newFullPath = Path.Combine(Directory.GetCurrentDirectory(), newFilePath);

            //檢查路徑是否存在，不存在則建立
            if (!Directory.Exists(newFullPath))
            {
                Directory.CreateDirectory(newFullPath);
            }

            var fileHash = GenerateFileMD5(fromFileFullPath);

            //檔案副檔名
            var extension = Path.GetExtension(fromFileFullPath);

            //新檔名
            var newFileName = GenerateFileName(fileHash, extension);

            //檔案完整路徑
            var newFileFullPath = Path.Combine(newFullPath, newFileName);

            var provider = new FileExtensionContentTypeProvider();

            // Try to get the MIME type of the file
            if (!provider.TryGetContentType(fromFileFullPath, out var contentType))
            {
                contentType = "application/octet-stream"; // 預設MIME類型
            }

            //獨動檔案
            File.Move(fromFileFullPath, newFileFullPath);

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

        //不檢查附檔名
        if (uploadOption.allowAllExtension)
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
        //允許副檔名
        if (uploadOption.allowExtension is { Count: 0 })
        {
            uploadOption.allowExtension =
            [
                ".txt", ".json", ".pdf", ".doc",
                ".xls", ".ppt", ".docx", ".xlsx", ".pptx"
            ];
        }

        //允許mime
        if (uploadOption.allowMimeType is { Count: 0 })
        {
            uploadOption.allowMimeType =
            [
                "text/plain", "application/json", "application/pdf", "application/msword",
                "application/vnd.ms-excel",
                "application/vnd.ms-powerpoint",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
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
        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), $"{BasePath}/{pathName}");

        //檢查路徑是否存在，不存在則建立
        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        try
        {
            // //依照檔案內容生成md5
            using var sha256 = SHA256.Create();
            var stream = formFile!.OpenReadStream();
            var hashData = sha256.ComputeHash(stream);
            var hashString = DataHelper.Byte2Hash(hashData);

            //新檔名
            var newFileName = GenerateFileName(hashString, Path.GetExtension(formFile.FileName));

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
    /// 檢查資料夾是否存在，不存在時會建立
    /// </summary>
    /// <param name="pathName"></param>
    public static void CheckPath(string pathName)
    {
        
        var projectRoot = Path.GetFullPath(Directory.GetCurrentDirectory());
        var fullPathToCreate = Path.GetFullPath(pathName);

        // 驗證：正規化後的路徑是否以專案根目錄開頭。
        // 這可以確保路徑不會遍歷到專案目錄之外。
        if (!fullPathToCreate.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid path specified. Path traversal is not allowed.");
        }

        if (string.IsNullOrEmpty(pathName))
        {
            return;
        }

        // 現在路徑是安全的，可以建立目錄
        if (!Directory.Exists(fullPathToCreate))
        {
            Directory.CreateDirectory(fullPathToCreate);
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
    public static CheckUploadFileModel CheckFile(
        IFormFile? uploadFile,
        List<string>? allowExtension = null,
        List<string>? allowMimeType = null,
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
        // 讀檔
        using var stream = File.OpenRead(filename);
        // 產生文件hash
        var hashData = MD5.HashData(stream);

        //返回檔案Hash
        return DataHelper.Byte2Hash(hashData);
    }

    /// <summary>
    /// 產生檔案名
    /// </summary>
    /// <param name="fileHashVal"></param>
    /// <param name="fileExtension"></param>
    /// <returns></returns>
    private static string GenerateFileName(string fileHashVal, string fileExtension)
    {
        var randomString = DataHelper.RandomString(9);
        var timestampSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        var cacheFileName = $"{fileHashVal}_{randomString}_{timestampSeconds}";

        // 將輸入的字串轉換成byte陣列
        var bytes = Encoding.UTF8.GetBytes(cacheFileName);

        // 計算SHA256 hash值
        var hashBytes = SHA256.HashData(bytes);

        return $"{DataHelper.Byte2Hash(hashBytes)}{fileExtension}";
    }
}