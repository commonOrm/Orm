using common.ConnectionProvider;
using common.ORM;
using common.ORM.LambdaToSQL;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;


public enum SQLSort
{
    /// <summary>
    /// SQL条件
    /// </summary>
    SQLWhere = 0,

    /// <summary>
    /// SQL排序
    /// </summary>
    SQLOrder = 1,

    /// <summary>
    /// SQL返回字段
    /// </summary>
    SQLFields = 2
}

/// <summary>
/// LambdaToSQL处理类
/// </summary>
public static class LambdaToSQLFactory
{
    public static List<SqlFuncExternal> loadExpMethods()
    {
        List<SqlFuncExternal> expMethods = new List<SqlFuncExternal>();
        expMethods.Add(new SqlFuncExternal()
        {
            UniqueMethodName = "lb_In",
            MethodValue = (expInfo, dbType, expContext) =>
            {
                return string.Format(" {0} in ({1}) ", expInfo.Args[0].MemberName, expInfo.Args[1].MemberName);
            }
        });
        expMethods.Add(new SqlFuncExternal()
        {
            UniqueMethodName = "lb_NotIn",
            MethodValue = (expInfo, dbType, expContext) =>
            {
                return string.Format(" {0} not in ({1}) ", expInfo.Args[0].MemberName, expInfo.Args[1].MemberName);
            }
        });
        expMethods.Add(new SqlFuncExternal()
        {
            UniqueMethodName = "lb_Like",
            MethodValue = (expInfo, dbType, expContext) =>
            {
                return string.Format(" {0} like concat('%',{1},'%') ", expInfo.Args[0].MemberName, expInfo.Args[1].MemberName);
            }
        });
        expMethods.Add(new SqlFuncExternal()
        {
            UniqueMethodName = "lb_LikeR",
            MethodValue = (expInfo, dbType, expContext) =>
            {
                return string.Format(" {0} like concat({1},'%') ", expInfo.Args[0].MemberName, expInfo.Args[1].MemberName);
            }
        });
        expMethods.Add(new SqlFuncExternal()
        {
            UniqueMethodName = "lb_LikeL",
            MethodValue = (expInfo, dbType, expContext) =>
            {
                return string.Format(" {0} like concat('%',{1}) ", expInfo.Args[0].MemberName, expInfo.Args[1].MemberName);
            }
        });
        expMethods.Add(new SqlFuncExternal()
        {
            UniqueMethodName = "lb_NotLike",
            MethodValue = (expInfo, dbType, expContext) =>
            {
                return string.Format(" {0} not like concat('%',{1},'%') ", expInfo.Args[0].MemberName, expInfo.Args[1].MemberName);
            }
        });
        expMethods.Add(new SqlFuncExternal()
        {
            UniqueMethodName = "lb_IsNotNullAndEqual",
            MethodValue = (expInfo, dbType, expContext) =>
            {
                if (expInfo.Args[1].MemberValue != null)
                    return string.Format(" {0}={1} ", expInfo.Args[0].MemberName, expInfo.Args[1].MemberName);
                else
                    return " 1=1 ";
            }
        });
        expMethods.Add(new SqlFuncExternal()
        {
            UniqueMethodName = "lb_IsNotNullAndDo",
            MethodValue = (expInfo, dbType, expContext) =>
            {
                if (expInfo.Args[1].MemberValue != null)
                    return string.Format(" {0}{2}{1} ", expInfo.Args[0].MemberName, expInfo.Args[1].MemberName, expInfo.Args[2].MemberValue);
                else
                    return " 1=1 ";
            }
        });
        expMethods.Add(new SqlFuncExternal()
        {
            UniqueMethodName = "lb_IsNotNullAndEmptyAndDo",
            MethodValue = (expInfo, dbType, expContext) =>
            {
                if (!string.IsNullOrWhiteSpace(expInfo.Args[1].MemberValue.ToString2()))
                    return string.Format(" {0}{2}{1} ", expInfo.Args[0].MemberName, expInfo.Args[1].MemberName, expInfo.Args[2].MemberValue);
                else
                    return " 1=1 ";
            }
        });
        expMethods.Add(new SqlFuncExternal()
        {
            UniqueMethodName = "lb_IsNotNullAndZeroAndDo",
            MethodValue = (expInfo, dbType, expContext) =>
            {
                if (expInfo.Args[1].MemberValue.ToInt32() != 0)
                    return string.Format(" {0}{2}{1} ", expInfo.Args[0].MemberName, expInfo.Args[1].MemberName, expInfo.Args[2].MemberValue);
                else
                    return " 1=1 ";
            }
        });
        expMethods.Add(new SqlFuncExternal()
        {
            UniqueMethodName = "lb_IsNotFalseAndEqual",
            MethodValue = (expInfo, dbType, expContext) =>
            {
                if ((bool)expInfo.Args[1].MemberValue != false)
                    return string.Format(" {0}={1} ", expInfo.Args[0].MemberName, expInfo.Args[1].MemberName);
                else
                    return " 1=1 ";
            }
        });
        expMethods.Add(new SqlFuncExternal()
        {
            UniqueMethodName = "lb_CheckNull",
            MethodValue = (expInfo, dbType, expContext) =>
            {
                int Value = expInfo.Args[1].MemberValue.ToInt32();
                switch (Value)
                {
                    //case 0:return " 1=1 ";
                    case 1: return string.Format(" {0} is null ", expInfo.Args[0].MemberName);
                    case 2: return string.Format(" {0} is not null ", expInfo.Args[0].MemberName);
                    case 3: return string.Format(" ({0} is null or {0} = '') ", expInfo.Args[0].MemberName);
                    case 4: return string.Format(" ({0} is not null and {0} <> '') ", expInfo.Args[0].MemberName);
                    default: return " 1=1 ";
                }
            }
        });
        return expMethods;
    }
    public static Dictionary<string, object> ConvertToDictionary(SqlParameter[] sqlParameters)
    {
        Dictionary<string, object> result = new Dictionary<string, object>();
        foreach (var param in sqlParameters)
            result.Add(param.ParameterName, param.Value);
        return result;
    }
    public static Dictionary<string, object> ConvertToDictionary(List<SqlParameter> sqlParameters)
    {
        return ConvertToDictionary(sqlParameters.ToArray());
    }
    public static SqlParameter[] ConverToSqlParameter(this List<SugarParameter> sugarParameters)
    {
        List<SqlParameter> sqlP = new List<SqlParameter>();
        foreach (var sp in sugarParameters)
            sqlP.Add(new SqlParameter(sp.ParameterName) { Value = sp.Value });
        return sqlP.ToArray();
    }
    public static SugarParameter[] ConverToSugarParameter(this Dictionary<string, object> sqlParameters)
    {
        List<SugarParameter> sqlP = new List<SugarParameter>();
        foreach (var sp in sqlParameters)
            sqlP.Add(new SugarParameter(sp.Key,sp.Value));
        return sqlP.ToArray();
    }

    #region Lambda扩展静态方法

    #region 字段

    /// <summary>
    /// 返回字段名
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool lb_ColumeName<T>(this T obj)
    {
        return true;
    }

    /// <summary>
    /// 字段和， sum(money)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool lb_Sum<T>(this T obj)
    {
        return true;
    }

    /// <summary>
    /// 字段平均值， SELECT AVG(OrderPrice) AS OrderAverage FROM Orders
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool lb_Avg<T>(this T obj)
    {
        return true;
    }

    /// <summary>
    /// 字段最小值， SELECT MIN(OrderPrice) AS OrderAverage FROM Orders
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool lb_Min<T>(this T obj)
    {
        return true;
    }

    /// <summary>
    /// 字段最大值， SELECT MAX(OrderPrice) AS OrderAverage FROM Orders
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool lb_Max<T>(this T obj)
    {
        return true;
    }

    /// <summary>
    /// 取出某列重复字段， SELECT DISTINCT 列名称 FROM 表名称
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool lb_Distinct<T>(this T obj)
    {
        return true;
    }

    #endregion 字段

    #region 排序

    /// <summary>
    /// Desc
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool lb_Desc<T>(this T obj)
    {
        return true;
    }

    /// <summary>
    /// Asc
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool lb_Asc<T>(this T obj)
    {
        return true;
    }

    /// <summary>
    /// 排序 Value(0:该字段不参与排序 1:Asc 2:Desc)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="Value"></param>
    /// <returns></returns>
    public static bool lb_OrderByValue<T>(this T obj, int Value)
    {
        return true;
    }

    public static bool lb_OrderByColumnAndValue<T>(this T obj, string Column, int Value)
        where T : ModelBase<T>, new()
    {
        return true;
    }

    /// <summary>
    /// Order By Arr 例子:  select * from table1 where id in (183,200) order by charindex(',' + ltrim(id) + ',' , ',183,200,')
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="array"></param>
    /// <returns></returns>
    public static bool lb_OrderByArr<T>(this T obj, int?[] array)
    {
        return true;
    }

    public static bool lb_Newid<T>(this T obj)
    {
        return true;
    }

    #endregion 排序

    #region 条件

    /// <summary>
    /// In
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="array"></param>
    /// <returns></returns>
    public static bool lb_In(this int obj, int[] array)
    {
        return true;
    }

    /// <summary>
    /// NotIn
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="array"></param>
    /// <returns></returns>
    public static bool lb_NotIn(this int obj, int[] array)
    {
        return true;
    }

    /// <summary>
    /// Like3ForInt - 不需传递%  生成 %,likeStr,% or %,likeStr or likeStr,% or =likeStr  --会自动抛弃null和0
    /// </summary>
    /// <param name="str"></param>
    /// <param name="likeStr"></param>
    /// <returns></returns>
    public static bool lb_Like4ForInt(this string str, object likeStr)
    {
        return true;
    }

    /// <summary>
    /// lb_Like4 - 不需传递%  生成 %,likeStr,% or %,likeStr or likeStr,% or =likeStr
    /// </summary>
    /// <param name="str"></param>
    /// <param name="likeStr"></param>
    /// <returns></returns>
    public static bool lb_Like4(this string str, string likeStr)
    {
        return true;
    }

    /// <summary>
    /// lb_Like4NotNullAndEmpty - 不需传递%  生成 %,likeStr,% or %,likeStr or likeStr,% or =likeStr --自动抛弃null和Empty
    /// </summary>
    /// <param name="str"></param>
    /// <param name="likeStr"></param>
    /// <returns></returns>
    public static bool lb_Like4NotNullAndEmpty(this object str, string likeStr)
    {
        return true;
    }

    /// <summary>
    /// Like - 不需传递%  生成 %likeStr%
    /// </summary>
    /// <param name="str"></param>
    /// <param name="likeStr"></param>
    /// <returns></returns>
    public static bool lb_Like(this string str, string likeStr)
    {
        return true;
    }

    /// <summary>
    /// LikeR - 不需传递%  生成 likeStr%
    /// </summary>
    /// <param name="str"></param>
    /// <param name="likeStr"></param>
    /// <returns></returns>
    public static bool lb_LikeR(this string str, string likeStr)
    {
        return true;
    }

    /// <summary>
    /// LikeL - 不需传递%  生成 %likeStr
    /// </summary>
    /// <param name="str"></param>
    /// <param name="likeStr"></param>
    /// <returns></returns>
    public static bool lb_LikeL(this string str, string likeStr)
    {
        return true;
    }

    /// <summary>
    /// NotLike
    /// </summary>
    /// <param name="str"></param>
    /// <param name="likeStr"></param>
    /// <returns></returns>
    public static bool lb_NotLike(this string str, string likeStr)
    {
        return true;
    }

    /// <summary>
    /// lb_LikeF - 不需传递%  生成 ,likeStr, like ','+Colume+','   ',语文,英文,数学,' like ',英文,'
    /// </summary>
    /// <param name="str"></param>
    /// <param name="likeStr"></param>
    /// <returns></returns>
    public static bool lb_LikeF(this string str, string[] likeStr)
    {
        return true;
    }


    /// <summary>
    /// 字符串如果不为null,则执行等于比较
    /// </summary>
    /// <param name="obj"></param>o
    /// <param name="Value"></param>
    /// <returns></returns>
    public static bool lb_IsNotNullAndEqual<T>(this T obj, string Value)
    {
        return true;
    }

    /// <summary>
    /// 执行字段null检查
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="Value">0:忽略该条件 1:查询为null数据 2:查询不为null数据 3:查询为null或空数据 4:查询不为null和不为空数据</param>
    /// <returns></returns>
    public static bool lb_CheckNull<T>(this T obj, int Value)
    {
        return true;
    }

    /// <summary>
    /// object如果不为null,则执行等于FuHao(参数二)比较
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="Value"></param>
    /// <param name="FuHao">比较符号</param>
    /// <returns></returns>
    public static bool lb_IsNotNullAndDo<T>(this T obj, object Value, string FuHao)
    {
        return true;
    }

    /// <summary>
    /// object如果不为null和Empty,则执行等于FuHao(参数二)比较
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="Value"></param>
    /// <param name="FuHao">比较符号</param>
    /// <returns></returns>
    public static bool lb_IsNotNullAndEmptyAndDo(this string obj, string Value, string FuHao)
    {
        return true;
    }

    /// <summary>
    /// object如果不为null和0,则执行等于FuHao(参数二)比较 针对于的数字型
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="Value"></param>
    /// <param name="FuHao">比较符号</param>
    /// <returns></returns>
    public static bool lb_IsNotNullAndZeroAndDo<T>(this T obj, object Value, string FuHao)
    {
        return true;
    }

    /// <summary>
    /// 如果传入false则不执行比较
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="Value"></param>
    /// <returns></returns>
    public static bool lb_IsNotFalseAndEqual<T>(this T obj, bool Value)
    {
        return true;
    }

    #endregion 条件

    #endregion Lambda扩展静态方法

    public static LambdaToSQLPlus Get<T>(SQLSort sqlsort, Expression<Func<T, bool>> func, SQLSign sqlsign, int? sign = null)
        where T : ModelBase<T>, new()
    {
        return new LambdaToSQLPlus(sqlsort, func, sqlsign, sign);
    }

    public static LambdaToSQLPlus Get<T, T2>(SQLSort sqlsort, Expression<Func<T, T2, bool>> func, SQLSign sqlsign, int? sign = null)
        where T : ModelBase<T>, new()
        where T2 : ModelBase<T2>, new()
    {
        return new LambdaToSQLPlus(sqlsort, func, sqlsign, sign);
    }

    public static LambdaToSQLPlus Get<T, T2, T3>(SQLSort sqlsort, Expression<Func<T, T2, T3, bool>> func, SQLSign sqlsign, int? sign = null)
        where T : ModelBase<T>, new()
        where T2 : ModelBase<T2>, new()
        where T3 : ModelBase<T3>, new()
    {
        return new LambdaToSQLPlus(sqlsort, func, sqlsign, sign);
    }

    public static LambdaToSQLPlus Get<T, T2, T3, T4>(SQLSort sqlsort, Expression<Func<T, T2, T3, T4, bool>> func, SQLSign sqlsign, int? sign = null)
        where T : ModelBase<T>, new()
        where T2 : ModelBase<T2>, new()
        where T3 : ModelBase<T3>, new()
        where T4 : ModelBase<T4>, new()
    {
        return new LambdaToSQLPlus(sqlsort, func, sqlsign, sign);
    }

    public static LambdaToSQLPlus Get<T, T2, T3, T4, T5>(SQLSort sqlsort, Expression<Func<T, T2, T3, T4, T5, bool>> func, SQLSign sqlsign, int? sign = null)
        where T : ModelBase<T>, new()
        where T2 : ModelBase<T2>, new()
        where T3 : ModelBase<T3>, new()
        where T4 : ModelBase<T4>, new()
        where T5 : ModelBase<T5>, new()
    {
        return new LambdaToSQLPlus(sqlsort, func, sqlsign, sign);
    }

    public static LambdaToSQLPlus Get<T, T2, T3, T4, T5, T6>(SQLSort sqlsort, Expression<Func<T, T2, T3, T4, T5, T6, bool>> func, SQLSign sqlsign, int? sign = null)
        where T : ModelBase<T>, new()
        where T2 : ModelBase<T2>, new()
        where T3 : ModelBase<T3>, new()
        where T4 : ModelBase<T4>, new()
        where T5 : ModelBase<T5>, new()
        where T6 : ModelBase<T6>, new()
    {
        return new LambdaToSQLPlus(sqlsort, func, sqlsign, sign);
    }
    public static LambdaToSQLPlus Get<T, T2, T3, T4, T5, T6, T7>(SQLSort sqlsort, Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>> func, SQLSign sqlsign, int? sign = null)
        where T : ModelBase<T>, new()
        where T2 : ModelBase<T2>, new()
        where T3 : ModelBase<T3>, new()
        where T4 : ModelBase<T4>, new()
        where T5 : ModelBase<T5>, new()
        where T6 : ModelBase<T6>, new()
        where T7 : ModelBase<T7>, new()
    {
        return new LambdaToSQLPlus(sqlsort, func, sqlsign, sign);
    }

}
