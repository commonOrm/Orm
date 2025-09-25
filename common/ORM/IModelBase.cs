using System;
using System.Threading.Tasks;
using Comm.ReactAdmin;
using System.Linq.Expressions;
using System.Data;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using common.ORM;
using SqlSugar;
using Dapper;
using System.Data.SqlClient;
using common.ORM.LambdaToSQL;
using SqlParameter = common.ORM.LambdaToSQL.SqlParameter;

public interface IModelBase<T> where T : ModelBase<T>, new()
{
    ModelBase<T> model { get; set; }

    Task<object> Add(SqlTranExtensions STE = null);
    Task<bool> Update(SqlTranExtensions STE = null);
    Task<bool> UpdateWhere(string set, string where, object param, SqlTranExtensions STE = null);

    Task<bool> UpdateWhere(Expression<Func<T, bool>> set, Expression<Func<T, bool>> where, SqlTranExtensions STE = null);
    Task<bool> UpdateByWhere(Expression<Func<T, T>> set, Expression<Func<T, bool>> where, SqlTranExtensions STE = null);

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
    /// ��ȡ����
    /// </summary>
    /// <returns></returns>
    protected string getTableName()
    {
        return typeof(T).Name;
    }

    /// <summary>
    /// ��ȡ�����ֶ�
    /// </summary>
    /// <returns></returns>
    protected PropertyInfo getPrimaryKeyColumn()
    {
        List<string> Columns = new List<string>();
        PropertyInfo[] propertyInfos = typeof(T).GetProperties();
        foreach (PropertyInfo pi in propertyInfos)
        {
            var key = pi.GetCustomAttribute(typeof(KeyAttribute)) as KeyAttribute;
            if (key != null)
                return pi;
            var sugarColumn = pi.GetCustomAttribute(typeof(SugarColumn)) as SugarColumn;
            if (sugarColumn != null && sugarColumn.IsPrimaryKey)
                return pi;
        }
        throw new MyException($"{getTableName()}û����������");
    }

    /// <summary>
    /// ��ȡ��������
    /// </summary>
    /// <returns></returns>
    protected string getPrimaryKeyName()
    {
        return getPrimaryKeyColumn().Name;
    }

    /// <summary>
    /// ��ȡ����ֵ
    /// </summary>
    /// <returns></returns>
    protected object getPrimaryKeyValue()
    {
        return getPrimaryKeyColumn().GetValue(model);
    }

    /// <summary>
    /// ��ȡ�����ֶ�
    /// </summary>
    /// <param name="excludePrimaryKey">�Ƿ��ų������ֶ�</param>
    /// <returns></returns>
    protected List<string> getColumns(string formatColumn = "{0}", bool excludePrimaryKey = true)
    {
        var primaryKeyName = getPrimaryKeyName();

        List<string> Columns = new List<string>();
        PropertyInfo[] propertyInfos = typeof(T).GetProperties();
        foreach (PropertyInfo pi in propertyInfos)
        {
            if (excludePrimaryKey && primaryKeyName == pi.Name) continue;

            Columns.Add(string.Format(formatColumn, pi.Name));
        }
        return Columns;
    }

    /// <summary>
    /// ���ֶ�ת���ַ���(����������)
    /// </summary>
    /// <param name="separator">�ָ��ַ�</param>
    /// <param name="formatColumn">��ʽ���ֶ�</param>
    /// <returns></returns>
    protected string getColumnsStringBySeparator(string separator = ",", string formatColumn = "{0}")
    {
        return string.Join(separator, getColumns(formatColumn));
    }

    /// <summary>
    /// ��ȡ�����ֶε�ֵ��Ĭ�ϲ�����������
    /// </summary>
    /// <param name="excludePrimaryKey"></param>
    /// <returns></returns>
    protected Dictionary<string, object> getColumnsValues(bool excludePrimaryKey = true)
    {
        var primaryKeyName = getPrimaryKeyName();
        var Dic = new Dictionary<string, object>();

        List<string> Columns = new List<string>();
        PropertyInfo[] propertyInfos = typeof(T).GetProperties();
        foreach (PropertyInfo pi in propertyInfos)
        {
            if (excludePrimaryKey && primaryKeyName == pi.Name) continue;

            var val = pi.GetValue(model);
            //���ݿ�������varchar�����ֵ��null���Զ���Ϊ�գ���ֹ����null
            if (val == null && pi.PropertyType == typeof(string)) val = "";
            Dic.Add(pi.Name, val);
        }
        return Dic;
    }


    /// <summary>
    /// ����Ƿ���ڼ�¼
    /// </summary>
    /// <param name="where"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    public async Task<bool> Exists(string where, object param)
    {
        if (string.IsNullOrWhiteSpace(where))
            where = "1=1";

        var orderby = @$" ""{getPrimaryKeyName()}"" ASC ";

        return (await new List2<T>(conn, getTableName(), where, param, 1, orderby).GetCount()) > 0;
    }


    /// <summary>
    /// ��ȡһ������ ����Ϊnull
    /// </summary>
    /// <param name="where"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    public async Task<T> GetModelWhere(string where, object param)
    {
        if (string.IsNullOrWhiteSpace(where))
            where = "1=1";

        var orderby = @$" ""{getPrimaryKeyName()}"" ASC ";

        var list = await new List2<T>(conn, getTableName(), where, param, 1, orderby).GetList();
        return list.Count > 0 ? list[0] : null;
    }



    /// <summary>
    /// ��ȡһ�����󼯺�
    /// </summary>
    /// <param name="where"></param>
    /// <param name="param"></param>
    /// <param name="top"></param>
    /// <param name="orderby"></param>
    /// <returns></returns>
    public List2<T> GetModelList(string where, object param, int top = int.MaxValue, string orderby = null)
    {
        if (string.IsNullOrWhiteSpace(where))
            where = "1=1";

        orderby = orderby ?? @$" ""{getPrimaryKeyName()}"" ASC ";
        return new List2<T>(conn, getTableName(), where, param, top, orderby);
    }


    public async Task<DataTable> GetFieldList(string fields, string where, object param, int top = int.MaxValue, string orderby = null)
    {
        if (string.IsNullOrWhiteSpace(fields))
            fields = "*";

        if (string.IsNullOrWhiteSpace(where))
            where = "1=1";

        orderby = orderby ?? @$" ""{getPrimaryKeyName()}"" ASC ";

        return await new List2<T>(conn, getTableName(), where, param, top, orderby, fields).GetDataTable();
    }





    /// <summary>
    /// ��ȡ��ҳ����
    /// </summary>
    /// <returns></returns>
    public PagerEx<T> Pager(string where, object param, int pageindex, int pagesize, string orderby = null)
    {
        if (string.IsNullOrWhiteSpace(where))
            where = "1=1";

        orderby = orderby ?? @$" ""{getPrimaryKeyName()}"" ASC ";
        return new PagerEx<T>(getTableName(), where, param, pageindex, pagesize, orderby);
    }

    /// <summary>
    /// ��ȡ��ҳ����
    /// </summary>
    /// <returns></returns>
    public PagerEx<T> Pager(string where, object param, int pageindex, int pagesize, string sort, SortBy order)
    {
        string orderby = string.IsNullOrWhiteSpace(sort) ? null : @$"""{sort}"" {order}";

        return Pager(where, param, pageindex, pagesize, orderby);
    }

}
