using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Xml.Serialization;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SampleProject.Base.Models.Http;
using SampleProject.Base.Models.Http.AuthType;
using SampleProject.Base.Models.Http.Form;

namespace SampleProject.Helpers;

public static class HttpHelper
{
    /// <summary>
    /// form表單傳送資料
    /// </summary>
    /// <param name="formDataList"></param>
    /// <param name="clientOption"></param>
    /// <returns></returns>
    public static async Task<ResponseModel?> FormRequest(List<FormContentModel> formDataList,
        ClientOptionModel? clientOption)
    {
        var responseData = new ResponseModel();

        if (formDataList is not { Count: > 0 })
        {
            return null;
        }

        clientOption ??= new ClientOptionModel
        {
            httpMethod = HttpMethod.Post
        };

        //建立httpClient
        using var client = InitHttpClient(clientOption);

        try
        {
            // 建立一個 HttpRequestMessage
            using var request = InitRequestMessage(clientOption);
            request.Content = InitFormContent(formDataList);

            // 發送請求
            var response = await client.SendAsync(request);
            responseData.statusCode = response.StatusCode;

            // 讀取並輸出回應
            var viewContent = await response.Content.ReadAsStringAsync();
            responseData.content = viewContent;

            //檢查內容是否為Json格式
            if (DataHelper.IsValidJson(viewContent))
            {
                responseData.responseContentType = ResponseContentType.JSON;
            }

            return responseData;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Request exception: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// raw request
    /// </summary>
    /// <param name="sendData">發送資料</param>
    /// <param name="clientOption">羨羨參數設定</param>
    /// <typeparam name="T">可帶入任何型別資料，會自動轉為Json</typeparam>
    /// <returns></returns>
    public static async Task<ResponseModel?> JsonContentRequest<T>(T? sendData, ClientOptionModel clientOption)
    {
        var responseData = new ResponseModel();

        //建立httpClient
        using var client = InitHttpClient(clientOption);

        // 建立一個 HttpRequestMessage
        using var request = InitRequestMessage(clientOption);

        //判斷是否需加入值Content
        if (sendData != null)
        {
            var json = sendData switch
            {
                string dataVal => dataVal,
                _ => JsonConvert.SerializeObject(sendData)
            };

            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
            request.Content = stringContent;
        }

        try
        {
            // 發送請求
            var response = await client.SendAsync(request);

            responseData.statusCode = response.StatusCode;

            // 讀取並輸出回應
            var viewContent = await response.Content.ReadAsStringAsync();
            responseData.content = viewContent;

            //檢查內容是否為Json格式
            if (DataHelper.IsValidJson(viewContent))
            {
                responseData.responseContentType = ResponseContentType.JSON;
            }

            return responseData;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Request exception: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// XML request
    /// </summary>
    /// <param name="sendData">XML 內容</param>
    /// <param name="clientOption">請求參數設定</param>
    /// <returns></returns>
    public static async Task<ResponseModel?> XmlContentRequest<T>(T? sendData, ClientOptionModel clientOption)
    {
        var responseData = new ResponseModel();

        //建立httpClient
        using var client = InitHttpClient(clientOption);

        // 建立一個 HttpRequestMessage
        using var request = InitRequestMessage(clientOption);

        //設置 XML 內容
        if (sendData != null)
        {
            string xmlContent;
            
            if (sendData is string dataVal)
            {
                xmlContent = dataVal;
            }
            else
            {
                var xmlSerializer = new XmlSerializer(typeof(T));
                await using var stringWriter = new StringWriter();
                xmlSerializer.Serialize(stringWriter, sendData);
                xmlContent = stringWriter.ToString();
            }

            if (!string.IsNullOrEmpty(xmlContent))
            {
                var stringContent = new StringContent(xmlContent, Encoding.UTF8, "application/xml");
                request.Content = stringContent;
            }
        }

        try
        {
            // 發送請求
            var response = await client.SendAsync(request);

            responseData.statusCode = response.StatusCode;

            // 讀取並輸出回應
            var viewContent = await response.Content.ReadAsStringAsync();
            responseData.content = viewContent;

            //檢查內容是否為XML格式
            if (DataHelper.IsValidXml(viewContent))
            {
                responseData.responseContentType = ResponseContentType.XML;
            }

            return responseData;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Request exception: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Url加上參數
    /// </summary>
    /// <param name="apiUrl"></param>
    /// <param name="urlParams"></param>
    /// <returns></returns>
    private static string UrlAddParams(string apiUrl, Dictionary<string, string> urlParams)
    {
        if (apiUrl is null or "")
        {
            return "";
        }

        var uriBuilder = new UriBuilder(apiUrl);

        if (urlParams is not { Keys.Count: > 0 })
        {
            return uriBuilder.ToString();
        }

        var parameters = HttpUtility.ParseQueryString(string.Empty);

        foreach (var urlParamsKey in urlParams.Keys)
        {
            var dataVal = urlParams[urlParamsKey];
            parameters[urlParamsKey] = dataVal;
        }

        uriBuilder.Query = parameters.ToString();

        return uriBuilder.ToString();
    }

    /// <summary>
    /// request加入自訂Header
    /// </summary>
    /// <param name="request"></param>
    /// <param name="headerParams"></param>
    private static void RequestAddHeader(ref HttpRequestMessage request, Dictionary<string, string> headerParams)
    {
        switch (headerParams)
        {
            case null:
            case { Keys.Count: > 0 }:
                return;
        }

        foreach (var headerKey in headerParams.Keys)
        {
            var headerVal = headerParams[headerKey];
            request.Headers.Add(headerKey, headerVal);
        }
    }

    /// <summary>
    /// HttpClient初始化
    /// </summary>
    /// <param name="clientOption"></param>
    /// <returns></returns>
    private static HttpClient InitHttpClient(ClientOptionModel clientOption)
    {
        var timeoutSec = clientOption.timeoutSec;
        var autoRedirect = clientOption.autoRedirect;

        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = autoRedirect, //依設定檔決定是否接收302時自動跳轉
        };

        PreAuth(clientOption.authModel, ref handler);

        var client = new HttpClient(handler);

        //數字異常，變為預設20
        if (timeoutSec <= 0)
        {
            timeoutSec = 20;
        }

        client.Timeout = TimeSpan.FromSeconds(timeoutSec);

        return client;
    }

    /// <summary>
    /// 初始化Request
    /// </summary>
    /// <param name="clientOption"></param>
    /// <returns></returns>
    private static HttpRequestMessage InitRequestMessage(ClientOptionModel clientOption)
    {
        // 使用的 API URL
        var requestUri = UrlAddParams(clientOption.requestApiUrl, clientOption.urlParams);

        // 建立一個 HttpRequestMessage
        var request = new HttpRequestMessage(clientOption.httpMethod, requestUri);

        //request加入自訂Header
        RequestAddHeader(ref request, clientOption.headerParams);

        //request加入驗證
        var authModel = clientOption.authModel;
        RequestMessageAuth(authModel, ref request);

        return request;
    }

    /// <summary>
    /// 先驗證模式
    /// </summary>
    /// <param name="authModel"></param>
    /// <param name="handler"></param>
    private static void PreAuth(AuthModel? authModel, ref HttpClientHandler handler)
    {
        if (authModel == null) return;
        var authType = authModel.authType?.ToLower() ?? "";

        switch (authType)
        {
            case "digest":
                if (authModel is DigestAuthModel digestAuthModel)
                {
                    handler.Credentials = new NetworkCredential(digestAuthModel.userName, digestAuthModel.password);
                    handler.PreAuthenticate = true;
                }

                break;
        }
    }

    /// <summary>
    /// 送出Request時健行驗證
    /// </summary>
    /// <param name="authModel"></param>
    /// <param name="request"></param>
    private static void RequestMessageAuth(AuthModel? authModel, ref HttpRequestMessage request)
    {
        if (authModel == null) return;
        var authType = authModel.authType?.ToLower() ?? "";

        switch (authType)
        {
            case "jwt":
                if (authModel is JwtAuthModel jwtAuthModel)
                {
                    var requestHeaderPrefix = jwtAuthModel.requestHeaderPrefix ?? "Bearer";
                    var token = jwtAuthModel.token ?? "";
                    request.Headers.Authorization = new AuthenticationHeaderValue(requestHeaderPrefix, token);
                }

                break;
        }
    }

    /// <summary>
    /// 初始化formContent
    /// </summary>
    /// <param name="formDataList"></param>
    /// <returns></returns>
    private static HttpContent InitFormContent(IReadOnlyCollection<FormContentModel> formDataList)
    {
        var haveFileContent = formDataList.FirstOrDefault(x => x.dataType == FormContentModel.DATA_TYPE_FILE);

        //有檔案，建立
        if (haveFileContent != null)
        {
            return InitMultipartFormDataContent(formDataList);

            HttpContent InitMultipartFormDataContent(IEnumerable<FormContentModel> formContentModels)
            {
                var dataFormContent = new MultipartFormDataContent();


                foreach (var formContent in formContentModels)
                {
                    var dataVal = formContent.dataVal.ToString();

                    if (dataVal == null)
                    {
                        continue;
                    }

                    //依照內容產生content
                    if (formContent.dataType == FormContentModel.DATA_TYPE_FILE)
                    {
                        //檢查檔案是否存在，檔案不存在不送
                        var checkFile = Path.Exists(dataVal);

                        if (!checkFile)
                        {
                            continue;
                        }

                        var fileStream = new FileStream(dataVal, FileMode.Open);

                        var fileContent = new StreamContent(fileStream);

                        var provider = new FileExtensionContentTypeProvider();

                        // Try to get the MIME type of the file
                        if (!provider.TryGetContentType(dataVal, out var contentType))
                        {
                            contentType = "application/octet-stream"; // 預設MIME類型
                        }

                        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

                        dataFormContent.Add(fileContent, formContent.dataKey, Path.GetFileName(dataVal));
                    }
                    else
                    {
                        var stringContent = new StringContent(dataVal);
                        stringContent.Headers.ContentDisposition =
                            new ContentDispositionHeaderValue("form-data") { Name = formContent.dataKey };
                        dataFormContent.Add(stringContent);
                    }
                }

                return dataFormContent;
            }
        }
        else
        {
            var formValues = formDataList.Select(formContent =>
                new KeyValuePair<string, string>(formContent.dataKey, formContent.dataVal + "")).ToList();

            return new FormUrlEncodedContent(formValues);
        }
    }
}