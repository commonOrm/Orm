using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public static class CommonFun
{
    public static bool IsNull(this object obj)
    {
        return obj == null;
    }

    public static string ToString2(this object obj)
    {
        try
        {
            return obj.ToString();
        }
        catch { return ""; }
    }

    public static int ToInt32(this object obj)
    {
        try
        {
            return Convert.ToInt32(obj.ToString2().ToDouble());
        }
        catch (Exception ce)
        {
            Console.WriteLine(ce.Message);
            return 0;
        }
    }
    public static double ToDouble(this object obj)
    {
        try
        {
            return Convert.ToDouble(obj.ToString2());
        }
        catch { return 0; }
    }
    public static Guid ToGuid(this object obj)
    {
        Guid Val = Guid.Empty;
        try { Val = Guid.Parse(obj.ToString2()); } catch { Val = Guid.Empty; }
        return Val;
    }

    /// <summary>
    /// 四舍五入
    /// </summary>
    /// <returns></returns>
    public static double JRound(this double value, int wei = 2)
    {
        decimal temp = (decimal)value;
        int decision = wei;
        temp = System.Decimal.Round(temp, decision);
        return temp.ToDouble();
    }

    public static string Jmd5(this string str)
    {
        //32位大写
        using (var md5 = MD5.Create())
        {
            var result = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
            var strResult = BitConverter.ToString(result);
            string result3 = strResult.Replace("-", "");
            //Console.WriteLine(result3);
            return result3;
        }
    }
     
}
