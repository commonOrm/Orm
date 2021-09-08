using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public static class CommonFun
{
    public static bool IsNull<T>(this ModelBase<T> model) where T : ModelBase<T>, new()
    {
        return model == null;
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

    public static string HttpToHttps(this string Url)
    {
        Url = Url.ToString2().Trim();
        Regex reg = new Regex("^http://", RegexOptions.IgnoreCase);
        return reg.Replace(Url, "https://");
    }
    public static string CreateNewUniqueID_PICI()
    {
        return DateTime.Now.ToString("yyyyMMddHHmmss") + RandomCode.GetNumChar(6).ToString2();
    }


    #region 得到数字加英文的随机数\得到英文的随机数\得到数字的随机数\得到中文的随机数
    /// <summary>
    /// 随机处理类
    /// 得到数字加英文的随机数\得到英文的随机数\得到数字的随机数\得到中文的随机数
    /// </summary>
    public static class RandomCode
    {
        private static char[] constant = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };

        /// <summary>
        /// 得到数字加英文的随机数，参数：随机数长度，返回值：一个字符串！
        /// </summary>
        /// <param name="strLength"></param>
        /// <returns></returns>
        public static string pxkt_GetCharFont(int strLength)
        {
            System.Text.StringBuilder newRandom = new System.Text.StringBuilder(62);
            Random rd = new Random();
            for (int i = 0; i < strLength; i++)
            {
                //线程休眠20毫秒
                System.Threading.Thread.Sleep(20);
                newRandom.Append(constant[rd.Next(62)]);
            }
            return newRandom.ToString();
        }


        private static char[] englishchar = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };

        /// <summary>
        /// 得到英文的随机数，参数：随机数长度，返回值：一个字符串！
        /// </summary>
        /// <param name="strLength"></param>
        /// <returns></returns>
        public static string GetEnglishChar(int strLength)
        {
            System.Text.StringBuilder newRandom = new System.Text.StringBuilder(52);
            Random rd = new Random();
            for (int i = 0; i < strLength; i++)
            {
                //线程休眠20毫秒
                System.Threading.Thread.Sleep(20);
                newRandom.Append(englishchar[rd.Next(52)]);

            }
            return newRandom.ToString();

        }


        private static char[] numchar = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };


        /// <summary>
        /// 得到数字的随机数，参数：随机数长度，返回值：一个字符串！
        /// </summary>
        /// <param name="strLength"></param>
        /// <returns></returns>
        public static string GetNumChar(int strLength)
        {

            System.Text.StringBuilder newRandom = new System.Text.StringBuilder(10);
            Random rd = new Random();
            for (int i = 0; i < strLength; i++)
            {
                //线程休眠20毫秒
                System.Threading.Thread.Sleep(20);
                newRandom.Append(numchar[rd.Next(10)]);
            }
            return newRandom.ToString();


        }

        /// <summary>
        /// 得到中文的随机数，参数：随机数长度，返回值：一个字符串！
        /// </summary>
        /// <param name="strLength"></param>
        /// <returns></returns>
        public static string GetChineseChar(int strLength)
        {

            System.Text.StringBuilder newRandom = new System.Text.StringBuilder();
            //获取GB2312编码页（表）
            System.Text.Encoding gb = System.Text.Encoding.GetEncoding("gb2312");
            //调用函数产生I个随机中文汉字编码
            object[] bytes = CreateRegionCode(strLength);

            for (int i = 0; i < strLength; i++)
            {
                //根据汉字编码的字节数组解码出中文汉字
                string str = gb.GetString((byte[])Convert.ChangeType(bytes[i], typeof(byte[])));
                newRandom.Append(str);

            }
            return newRandom.ToString();
        }

        private static object[] CreateRegionCode(int strlength)
        {
            //定义一个字符串数组储存汉字编码的组成元素
            string[] rBase = new String[16] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f" };


            Random rnd = new Random();


            //定义一个object数组用来
            object[] bytes = new object[strlength];



            /**/
            /*每循环一次产生一个含两个元素的十六进制字节数组，并将其放入bject数组中
             每个汉字有四个区位码组成
             区位码第1位和区位码第2位作为字节数组第一个元素
             区位码第3位和区位码第4位作为字节数组第二个元素
            */
            for (int i = 0; i < strlength; i++)
            {
                //区位码第1位
                int r1 = rnd.Next(11, 14);
                string str_r1 = rBase[r1].Trim();


                //区位码第2位
                rnd = new Random(r1 * unchecked((int)DateTime.Now.Ticks) + i);//更换随机数发生器的种子避免产生重复值
                int r2;
                if (r1 == 13)
                {
                    r2 = rnd.Next(0, 7);
                }
                else
                {
                    r2 = rnd.Next(0, 16);
                }
                string str_r2 = rBase[r2].Trim();

                //区位码第3位
                rnd = new Random(r2 * unchecked((int)DateTime.Now.Ticks) + i);
                int r3 = rnd.Next(10, 16);
                string str_r3 = rBase[r3].Trim();

                //区位码第4位
                rnd = new Random(r3 * unchecked((int)DateTime.Now.Ticks) + i);
                int r4;
                if (r3 == 10)
                {
                    r4 = rnd.Next(1, 16);
                }
                else if (r3 == 15)
                {
                    r4 = rnd.Next(0, 15);
                }
                else
                {
                    r4 = rnd.Next(0, 16);
                }
                string str_r4 = rBase[r4].Trim();

                //定义两个字节变量存储产生的随机汉字区位码
                byte byte1 = Convert.ToByte(str_r1 + str_r2, 16);
                byte byte2 = Convert.ToByte(str_r3 + str_r4, 16);
                //将两个字节变量存储在字节数组中
                byte[] str_r = new byte[] { byte1, byte2 };

                //将产生的一个汉字的字节数组放入object数组中
                bytes.SetValue(str_r, i);

            }

            return bytes;

        }

    }
    #endregion
}
