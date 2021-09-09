using System;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using Dapper;
using System.Threading.Tasks;
using Comm.ReactAdmin;
using System.Linq.Expressions;
using common.ORM.LambdaToSQL;
using System.Data;
using common.ConnectionProvider;
using common.ORM;
using Microsoft.Extensions.Configuration;

public static class ServiceLocator
{
    public static IServiceProvider Instance { get; set; }
}

public class ModelBase<T> where T : ModelBase<T>, new()
{
    public static IConnectionProvider conn { get; set; }
    public static SQLSign sqlsign { get; set; }

    public ModelBase()
    {
        init();
    }
    static ModelBase()
    {
        init();
    }

    static void init()
    {
        IConfiguration configuration = ServiceLocator.Instance.GetService(typeof(IConfiguration)) as IConfiguration;
        if (conn == null || configuration["ASPNETCORE_ENVIRONMENT"].ToLower() == "Development".ToLower())
        {
            if (ServiceLocator.Instance == null) throw new MyException("ServiceLocator.Instance is Null");
            conn = ServiceLocator.Instance.GetService(typeof(IConnectionProvider)) as IConnectionProvider;

            sqlsign = SQLSign.Create(conn);
        }
    }



    /// <summary>
    /// 获取表名
    /// </summary>
    /// <returns></returns>
    private static string getTableName()
    {
        return typeof(T).Name;
    }

    /// <summary>
    /// 获取主键字段
    /// </summary>
    /// <returns></returns>
    private static PropertyInfo getPrimaryKeyColumn()
    {
        List<string> Columns = new List<string>();
        PropertyInfo[] propertyInfos = typeof(T).GetProperties();
        foreach (PropertyInfo pi in propertyInfos)
        {
            if (pi.GetCustomAttribute(typeof(KeyAttribute)) != null)
                return pi;
        }
        throw new MyException($"{getTableName()}没有设置主键");
    }

    /// <summary>
    /// 获取主键名称
    /// </summary>
    /// <returns></returns>
    private static string getPrimaryKeyName()
    {
        return getPrimaryKeyColumn().Name;
    }

    /// <summary>
    /// 获取主键值
    /// </summary>
    /// <returns></returns>
    private object getPrimaryKeyValue()
    {
        return getPrimaryKeyColumn().GetValue(this);
    }

    /// <summary>
    /// 获取所有字段
    /// </summary>
    /// <param name="excludePrimaryKey">是否排除主键字段</param>
    /// <returns></returns>
    private static List<string> getColumns(string formatColumn = "{0}", bool excludePrimaryKey = true)
    {
        List<string> Columns = new List<string>();
        PropertyInfo[] propertyInfos = typeof(T).GetProperties();
        foreach (PropertyInfo pi in propertyInfos)
        {
            if (excludePrimaryKey)
            {
                if (pi.GetCustomAttribute(typeof(KeyAttribute)) != null)
                    continue;
            }
            Columns.Add(string.Format(formatColumn, pi.Name));
        }
        return Columns;
    }

    /// <summary>
    /// 把字段转成字符串(不包含主键)
    /// </summary>
    /// <param name="separator">分割字符</param>
    /// <param name="formatColumn">格式化字段</param>
    /// <returns></returns>
    private static string getColumnsStringBySeparator(string separator = ",", string formatColumn = "{0}")
    {
        return string.Join(separator, getColumns(formatColumn));
    }

    /// <summary>
    /// 获取所有字段的值（默认不包含主键）
    /// </summary>
    /// <param name="excludePrimaryKey"></param>
    /// <returns></returns>
    private Dictionary<string, object> getColumnsValues(bool excludePrimaryKey = true)
    {
        var Dic = new Dictionary<string, object>();

        List<string> Columns = new List<string>();
        PropertyInfo[] propertyInfos = typeof(T).GetProperties();
        foreach (PropertyInfo pi in propertyInfos)
        {
            if (excludePrimaryKey)
            {
                if (pi.GetCustomAttribute(typeof(KeyAttribute)) != null)
                    continue;
            }
            var val = pi.GetValue(this);
            //数据库里面是varchar，如果值是null就自动变为空，防止存入null
            if (val == null && pi.PropertyType == typeof(string)) val = "";
            Dic.Add(pi.Name, val);
        }
        return Dic;
    }



    /// <summary>
    /// 新增
    /// </summary>
    /// <returns></returns>
    public async Task<object> Add(SqlTranExtensions STE = null)
    {
        string columns = getColumnsStringBySeparator(",", sqlsign.Create_ColumnEx("{0}"));
        string column_args = getColumnsStringBySeparator(",", "@{0}");
        Dictionary<string, object> column_vals = getColumnsValues();

        var sql = sqlsign.Create_InsertIntoSQLEx(getTableName(), columns, column_args, getPrimaryKeyName());

        if (STE != null)
        {
            return await STE.connection.ExecuteScalarAsync(sql, column_vals, STE.transaction);
        }
        else
            using (var connection = conn.GetDbConnection())
            {
                return await connection.ExecuteScalarAsync(sql, column_vals);
            }
    }

    /// <summary>
    /// 更新
    /// </summary>
    /// <returns></returns>
    public async Task<bool> Update(SqlTranExtensions STE = null)
    {
        string column_args = getColumnsStringBySeparator(",", "\"{0}\"=@{0}");
        Dictionary<string, object> column_vals = getColumnsValues(false);

        var sql = $@"UPDATE ""{getTableName()}"" SET {column_args} WHERE ""{getPrimaryKeyName()}""=@{getPrimaryKeyName()};";
        if (STE != null)
        {
            var result = await STE.connection.ExecuteAsync(sql, column_vals, STE.transaction);
            return result > 0;
        }
        else
            using (var connection = conn.GetDbConnection())
            {
                var result = await connection.ExecuteAsync(sql, column_vals);
                return result > 0;
            }
    }

    /// <summary>
    /// 更新 按条件（多条）
    /// </summary>
    /// <param name="set"></param>
    /// <param name="where"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    public static async Task<bool> UpdateWhere(string set, string where, object param, SqlTranExtensions STE = null)
    {
        if (string.IsNullOrWhiteSpace(where))
            throw new MyException($"【UpdateWhere】 where 参数不能为空");

        var sql = $@"UPDATE ""{getTableName()}"" SET {set} WHERE {where};";
        if (STE != null)
        {
            var result = await STE.connection.ExecuteAsync(sql, param, STE.transaction);
            return result > 0;
        }
        else
            using (var connection = conn.GetDbConnection())
            {
                var result = await connection.ExecuteAsync(sql, param);
                return result > 0;
            }
    }
    public static async Task<bool> UpdateWhere(Expression<Func<T, bool>> set, Expression<Func<T, bool>> where, SqlTranExtensions STE = null)
    {
        var setResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, set, 5000);
        var whereResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, where);
        var param = new List<SqlParameter>();
        param.AddRange(setResult.Lambda_SPArr);
        param.AddRange(whereResult.Lambda_SPArr);
        return await UpdateWhere(setResult.Lambda_Sql.Replace("(", "").Replace(")", "").Replace("AND", ","), whereResult.Lambda_Sql, LambdaToSQLFactory.ConvertToDictionary(param), STE);
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <returns></returns>
    public async Task<bool> Delete(SqlTranExtensions STE = null)
    {
        Dictionary<string, object> column_vals = new Dictionary<string, object>();
        column_vals.Add(getPrimaryKeyName(), getPrimaryKeyValue());

        var sql = $@"DELETE FROM ""{getTableName()}"" WHERE ""{getPrimaryKeyName()}""=@{getPrimaryKeyName()} ;";
        if (STE != null)
        {
            var result = await STE.connection.ExecuteAsync(sql, column_vals, STE.transaction);
            return result > 0;
        }
        else
            using (var connection = conn.GetDbConnection())
            {
                var result = await connection.ExecuteAsync(sql, column_vals);
                return result > 0;
            }
    }

    /// <summary>
    /// 删除 按条件（多条）
    /// </summary>
    /// <param name="where"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    public static async Task<bool> DeleteWhere(string where, object param, SqlTranExtensions STE = null)
    {
        if (string.IsNullOrWhiteSpace(where))
            throw new MyException($"【DeleteWhere】 where 参数不能为空");

        var sql = $@"DELETE FROM ""{getTableName()}"" WHERE {where} ;";
        if (STE != null)
        {
            var result = await STE.connection.ExecuteAsync(sql, param, STE.transaction);
            return result > 0;
        }
        else
            using (var connection = conn.GetDbConnection())
            {
                var result = await connection.ExecuteAsync(sql, param);
                return result > 0;
            }
    }
    public static async Task<bool> DeleteWhere(Expression<Func<T, bool>> where, SqlTranExtensions STE = null)
    {
        var whereResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, where);
        return await DeleteWhere(whereResult.Lambda_Sql, LambdaToSQLFactory.ConvertToDictionary(whereResult.Lambda_SPArr), STE);
    }

    /// <summary>
    /// 获取一个对象 可能为null
    /// </summary>
    /// <param name="PrimaryKeyValue"></param>
    /// <returns></returns>
    public static async Task<T> GetModel(object PrimaryKeyValue)
    {
        Dictionary<string, object> column_vals = new Dictionary<string, object>();
        column_vals.Add(getPrimaryKeyName(), PrimaryKeyValue);
        using (var connection = conn.GetDbConnection())
        {
            return await connection.QueryFirstOrDefaultAsync<T>(@$"SELECT * FROM ""{getTableName()}"" 
                        WHERE ""{getPrimaryKeyName()}""=@{getPrimaryKeyName()}", column_vals);
        }
    }
    /// <summary>
    /// 获取一个对象 可能为null
    /// </summary>
    /// <param name="where"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    public static async Task<T> GetModelWhere(string where, object param)
    {
        if (string.IsNullOrWhiteSpace(where))
            where = "1=1";

        using (var connection = conn.GetDbConnection())
        {
            return await connection.QueryFirstOrDefaultAsync<T>(@$"SELECT * FROM ""{getTableName()}"" WHERE {where}", param);
        }
    }
    public static async Task<T> GetModelWhere(Expression<Func<T, bool>> where)
    {
        var whereResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, where);
        return await GetModelWhere(whereResult.Lambda_Sql, LambdaToSQLFactory.ConvertToDictionary(whereResult.Lambda_SPArr));
    }
    /// <summary>
    /// 检查是否存在记录
    /// </summary>
    /// <param name="where"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    public static async Task<bool> Exists(string where, object param)
    {
        if (string.IsNullOrWhiteSpace(where))
            where = "1=1";

        using (var connection = conn.GetDbConnection())
        {
            return await connection.ExecuteScalarAsync<bool>(@$"SELECT COUNT(1) FROM ""{getTableName()}"" WHERE {where}", param);
        }
    }
    public static async Task<bool> Exists(Expression<Func<T, bool>> where)
    {
        var whereResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, where);
        return await Exists(whereResult.Lambda_Sql, LambdaToSQLFactory.ConvertToDictionary(whereResult.Lambda_SPArr));
    }
    /// <summary>
    /// 获取一个对象集合
    /// </summary>
    /// <param name="where"></param>
    /// <param name="param"></param>
    /// <param name="top"></param>
    /// <param name="orderby"></param>
    /// <returns></returns>
    public static List2<T> GetModelList(string where, object param, int top = int.MaxValue, string orderby = null)
    {
        if (string.IsNullOrWhiteSpace(where))
            where = "1=1";

        orderby = orderby ?? @$" ""{getPrimaryKeyName()}"" ASC ";
        return new List2<T>(conn, getTableName(), where, param, top, orderby);
    }
    public static List2<T> GetModelList(Expression<Func<T, bool>> where, int top = int.MaxValue, Expression<Func<T, bool>> orderby = null)
    {
        var whereResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, where);
        var orderbyResult = orderby == null ? null : LambdaToSQLFactory.Get<T>(SQLSort.SQLOrder, orderby);
        return GetModelList(whereResult.Lambda_Sql, LambdaToSQLFactory.ConvertToDictionary(whereResult.Lambda_SPArr), top, orderby == null ? null : orderbyResult.Lambda_Sql);
    }
    public static async Task<DataTable> GetFieldList(string fields, string where, object param, int top = int.MaxValue, string orderby = null)
    {
        if (string.IsNullOrWhiteSpace(fields))
            fields = "*";

        if (string.IsNullOrWhiteSpace(where))
            where = "1=1";

        orderby = orderby ?? @$" ""{getPrimaryKeyName()}"" ASC ";

        //return new List2<T>(conn, getTableName(), where, param, top, orderby);
        using (var connection = conn.GetDbConnection())
        {
            var tablename = getTableName();
            DataTable table = new DataTable("MyTable");
            var sql = sqlsign.Create_GetListSQLEx(fields, tablename, where, orderby, top);
            var reader = await connection.ExecuteReaderAsync(sql, param);
            table.Load(reader);
            return table;
        }
    }
    public static async Task<DataTable> GetFieldList(Expression<Func<T, bool>> fields, Expression<Func<T, bool>> where, int top = int.MaxValue, Expression<Func<T, bool>> orderby = null)
    {
        var fieldsResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLFields, fields);
        var whereResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, where);
        var orderbyResult = orderby == null ? null : LambdaToSQLFactory.Get<T>(SQLSort.SQLOrder, orderby);
        return await GetFieldList(fieldsResult.Lambda_Sql, whereResult.Lambda_Sql, LambdaToSQLFactory.ConvertToDictionary(whereResult.Lambda_SPArr), top, orderby == null ? null : orderbyResult.Lambda_Sql);
    }


    /// <summary>
    /// 获取分页对象
    /// </summary>
    /// <returns></returns>
    public static PagerEx<T> Pager(string where, object param, int pageindex, int pagesize, string orderby = null)
    {
        if (string.IsNullOrWhiteSpace(where))
            where = "1=1";

        orderby = orderby ?? @$" ""{getPrimaryKeyName()}"" ASC ";
        return new PagerEx<T>(getTableName(), where, param, pageindex, pagesize, orderby);
    }
    public static PagerEx<T> Pager(Expression<Func<T, bool>> where, int pageindex, int pagesize, Expression<Func<T, bool>> orderby = null)
    {
        var whereResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, where);
        var orderbyResult = orderby == null ? null : LambdaToSQLFactory.Get<T>(SQLSort.SQLOrder, orderby);
        return Pager(whereResult.Lambda_Sql, LambdaToSQLFactory.ConvertToDictionary(whereResult.Lambda_SPArr), pageindex, pagesize, orderby == null ? null : orderbyResult.Lambda_Sql);
    }
    /// <summary>
    /// 获取分页对象
    /// </summary>
    /// <returns></returns>
    public static PagerEx<T> Pager(string where, object param, int pageindex, int pagesize, string sort, SortBy order)
    {
        string orderby = string.IsNullOrWhiteSpace(sort) ? null : @$"""{sort}"" {order}";

        return Pager(where, param, pageindex, pagesize, orderby);
    }
    public static PagerEx<T> Pager(Expression<Func<T, bool>> where, int pageindex, int pagesize, string sort, SortBy order)
    {
        var whereResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, where);
        string orderby = string.IsNullOrWhiteSpace(sort) ? null : @$"""{sort}"" {order}";

        return Pager(whereResult.Lambda_Sql, LambdaToSQLFactory.ConvertToDictionary(whereResult.Lambda_SPArr), pageindex, pagesize, orderby);
    }
}