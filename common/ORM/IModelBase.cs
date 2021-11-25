using System;
using System.Threading.Tasks;
using Comm.ReactAdmin;
using System.Linq.Expressions;
using System.Data;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using common.ORM;

public interface IModelBase<T> where T : ModelBase<T>, new()
{
    ModelBase<T> model { get; set; }

    Task<object> Add(SqlTranExtensions STE = null);
    Task<bool> Update(SqlTranExtensions STE = null);
    Task<bool> UpdateWhere(string set, string where, object param, SqlTranExtensions STE = null);
    Task<bool> UpdateWhere(Expression<Func<T, bool>> set, Expression<Func<T, bool>> where, SqlTranExtensions STE = null);
    Task<bool> Delete(SqlTranExtensions STE = null);
    Task<bool> DeleteWhere(string where, object param, SqlTranExtensions STE = null);
    Task<bool> DeleteWhere(Expression<Func<T, bool>> where, SqlTranExtensions STE = null);
    Task<T> GetModel(object PrimaryKeyValue);
    Task<T> GetModelWhere(string where, object param);
    Task<T> GetModelWhere(Expression<Func<T, bool>> where);
    Task<bool> Exists(string where, object param);
    Task<bool> Exists(Expression<Func<T, bool>> where);
    List2<T> GetModelList(string where, object param, int top = int.MaxValue, string orderby = null);
    List2<T> GetModelList(Expression<Func<T, bool>> where, int top = int.MaxValue, Expression<Func<T, bool>> orderby = null);
    Task<DataTable> GetFieldList(string fields, string where, object param, int top = int.MaxValue, string orderby = null);
    Task<DataTable> GetFieldList(Expression<Func<T, bool>> fields, Expression<Func<T, bool>> where, int top = int.MaxValue, Expression<Func<T, bool>> orderby = null);
    PagerEx<T> Pager(string where, object param, int pageindex, int pagesize, string orderby = null);
    PagerEx<T> Pager(Expression<Func<T, bool>> where, int pageindex, int pagesize, Expression<Func<T, bool>> orderby = null);
    PagerEx<T> Pager(string where, object param, int pageindex, int pagesize, string sort, SortBy order);
    PagerEx<T> Pager(Expression<Func<T, bool>> where, int pageindex, int pagesize, string sort, SortBy order);
}

public abstract class ModelBaseAbs<T> where T : ModelBase<T>, new()
{
    public ModelBase<T> model { get; set; }
    protected IConnectionProvider conn { get; set; }
    protected SQLSign sqlsign { get; set; }

    public ModelBaseAbs()
    {
        if (ServiceLocator.Instance == null) throw new MyException("ServiceLocator.Instance is Null");
        if (conn == null)
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
    protected string getTableName()
    {
        return typeof(T).Name;
    }

    /// <summary>
    /// 获取主键字段
    /// </summary>
    /// <returns></returns>
    protected PropertyInfo getPrimaryKeyColumn()
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
    protected string getPrimaryKeyName()
    {
        return getPrimaryKeyColumn().Name;
    }

    /// <summary>
    /// 获取主键值
    /// </summary>
    /// <returns></returns>
    protected object getPrimaryKeyValue()
    {
        return getPrimaryKeyColumn().GetValue(model);
    }

    /// <summary>
    /// 获取所有字段
    /// </summary>
    /// <param name="excludePrimaryKey">是否排除主键字段</param>
    /// <returns></returns>
    protected List<string> getColumns(string formatColumn = "{0}", bool excludePrimaryKey = true)
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
    protected string getColumnsStringBySeparator(string separator = ",", string formatColumn = "{0}")
    {
        return string.Join(separator, getColumns(formatColumn));
    }

    /// <summary>
    /// 获取所有字段的值（默认不包含主键）
    /// </summary>
    /// <param name="excludePrimaryKey"></param>
    /// <returns></returns>
    protected Dictionary<string, object> getColumnsValues(bool excludePrimaryKey = true)
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
            var val = pi.GetValue(model);
            //数据库里面是varchar，如果值是null就自动变为空，防止存入null
            if (val == null && pi.PropertyType == typeof(string)) val = "";
            Dic.Add(pi.Name, val);
        }
        return Dic;
    }

}
