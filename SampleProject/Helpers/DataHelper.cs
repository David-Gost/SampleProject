using System.Text;
using System.Xml;
using Newtonsoft.Json;

namespace SampleProject.Helpers;

public static class DataHelper
{
    /// <summary>
    /// 產生亂數
    /// </summary>
    /// <param name="length"></param>
    /// <param name="elements"></param>
    /// <returns></returns>
    public static string RandomString(int length = 6, string[]? elements = null)
    {
        elements ??= new string[] { "AZ", "az", "9" };

        //定義字典表
        var sets = new Dictionary<string, string>()
        {
            { "AZ", "QWERTYUIOPASDFGHJKLZXCVBNM" },
            { "az", "qwertyuiopasdfghjklzxcvbnm" },
            { "9", "0123456789" },
            { "@", "~!@#$%^&*+-_=" },
            { "()", "(){}[]" }
        };

        var chars = new StringBuilder();

        foreach (var elem in elements)
        {
            if (sets.TryGetValue(elem, out var value))
            {
                chars.Append(value);
            }
        }

        var finalChars = chars.ToString();
        var stringChars = new char[length];
        var random = new Random();

        //依照長度＆字典產生亂數
        for (var i = 0; i < stringChars.Length; i++)
        {
            stringChars[i] = finalChars[random.Next(finalChars.Length)];
        }

        return new string(stringChars);
    }

    /// <summary>
    /// Byte轉換成Hash字串
    /// </summary>
    /// <param name="byteArray"></param>
    /// <param name="formatType">x2:輸出小寫（預設值），X2輸出大寫</param>
    /// <returns></returns>
    public static string Byte2Hash(byte[] byteArray, string formatType = "x2")
    {
        switch (byteArray.Length)
        {
            default:
                return "";
            case > 0:
            {
                var hashVal = BitConverter.ToString(byteArray).Replace("-", "");
                return formatType switch
                {
                    "X2" => hashVal.ToUpperInvariant(),
                    _ => hashVal.ToLowerInvariant()
                };
            }
        }
    }
    
    /// <summary>
    /// 檢查內容是否為Json
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static bool IsValidJson(string input)
    {
        input = input.Trim();
        if ((input.StartsWith($"{{") && input.EndsWith($"}}")) || ( //For object
                input.StartsWith($"[") && input.EndsWith($"]"))) //For array
        {
            try
            {
                var obj = Newtonsoft.Json.Linq.JToken.Parse(input);
                return true;
            }
            catch (JsonReaderException jex)
            {
                //Exception in parsing json
                Console.WriteLine(jex.Message);
                return false;
            }
            catch (Exception ex) //some other exception
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        else
        {
            return false;
        }
    }
    
    /// <summary>
    /// 檢查是否為XML格式
    /// </summary>
    /// <param name="xml"></param>
    /// <returns></returns>
    public static bool IsValidXml(string xml)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return true;
        }
        catch
        {
            return false;
        }
    }
}