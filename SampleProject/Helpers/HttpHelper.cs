using System.Diagnostics.Metrics;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SampleProject.Base.Models.Http;

namespace SampleProject.Helpers;

public static class HttpHelper
{
    
    /// <summary>
    /// raw request
    /// </summary>
    /// <param name="sendData">發送資料</param>
    /// <param name="clientOption">羨羨參數設定</param>
    /// <typeparam name="T">可帶入任何型別資料，會自動轉為Json</typeparam>
    /// <returns></returns>
    public static async Task<ResponseModel?> RawContentRequest<T>(T? sendData, ClientOptionModel clientOption)
    {
        var responseData = new ResponseModel();
        using var client = new HttpClient();
        // 使用的 API URL
        var requestUri = UrlAddParams(clientOption.requestApiUrl,clientOption.urlParams);

        var jwtToken = clientOption.bearerToken ?? "";

        // 建立一個 HttpRequestMessage
        var request = new HttpRequestMessage(clientOption.httpMethod, requestUri);
        
        //request加入自訂Header
        RequestAddHeader(ref request, clientOption.headerParams);

        //header加入Bearer Token
        if (!jwtToken.Equals(""))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
        }

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

            responseData.statusCode =  response.StatusCode;
            
            // 讀取並輸出回應
            var viewContent=await response.Content.ReadAsStringAsync();
            responseData.content = viewContent;

            //檢查內容是否為Json格式
            if (DataHelper.IsValidJson(viewContent))
            {
                responseData.isJsonData = true;
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
        foreach (var headerKey in headerParams.Keys)
        {
            var headerVal = headerParams[headerKey];
            request.Headers.Add(headerKey,headerVal);
        }
    }
}