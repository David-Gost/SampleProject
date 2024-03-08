using System.Text;

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
            { "9",  "0123456789" },
            { "@",  "~!@#$%^&*+-_=" },
            { "()", "(){}[]" }
        };

        var chars = new StringBuilder();

        foreach(var elem in elements)
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
}