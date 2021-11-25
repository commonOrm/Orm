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
using Microsoft.Extensions.Logging;
using SqlSugar;

public static class ServiceLocator
{
    public static IServiceProvider Instance { get; set; }
}


public class ModelBase<T> where T : ModelBase<T>, new()
{
    [SugarColumn(IsIgnore = true)]
    private IModelBase<T> modelBase { get; set; }

    public ModelBase()
    {
        //ILogger<ModelBase<T>> logger = ServiceLocator.Instance.GetService(typeof(ILogger<ModelBase<T>>))
        if (ServiceLocator.Instance == null) throw new MyException("ServiceLocator.Instance is Null");
        if (modelBase == null)
        {
            modelBase = ServiceLocator.Instance.GetService(typeof(IModelBase<T>)) as IModelBase<T>;
            modelBase.model = this;
        }
    }

    static IModelBase<T> getModelBase()
    {
        return ServiceLocator.Instance.GetService(typeof(IModelBase<T>)) as IModelBase<T>;
    }

    /// <summary>
    /// 新增
    /// </summary>
    /// <returns></returns>
    public async Task<object> Add(SqlTranExtensions STE = null)
    {
        return await modelBase.Add(STE);
    }

    /// <summary>
    /// 更新
    /// </summary>
    /// <returns></returns>
    public async Task<bool> Update(SqlTranExtensions STE = null)
    {
        return await modelBase.Update(STE);
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
        return await getModelBase().UpdateWhere(set, where, param, STE);
    }
    public static async Task<bool> UpdateWhere(Expression<Func<T, bool>> set, Expression<Func<T, bool>> where, SqlTranExtensions STE = null)
    {
        return await getModelBase().UpdateWhere(set, where, STE);
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <returns></returns>
    public async Task<bool> Delete(SqlTranExtensions STE = null)
    {
        return await modelBase.Delete(STE);
    }

    /// <summary>
    /// 删除 按条件（多条）
    /// </summary>
    /// <param name="where"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    public static async Task<bool> DeleteWhere(string where, object param, SqlTranExtensions STE = null)
    {
        return await getModelBase().DeleteWhere(where, param, STE);
    }
    public static async Task<bool> DeleteWhere(Expression<Func<T, bool>> where, SqlTranExtensions STE = null)
    {
        return await getModelBase().DeleteWhere(where, STE);
    }

    /// <summary>
    /// 获取一个对象 可能为null
    /// </summary>
    /// <param name="PrimaryKeyValue"></param>
    /// <returns></returns>
    public static async Task<T> GetModel(object PrimaryKeyValue)
    {
        return await getModelBase().GetModel(PrimaryKeyValue);
    }
    /// <summary>
    /// 获取一个对象 可能为null
    /// </summary>
    /// <param name="where"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    public static async Task<T> GetModelWhere(string where, object param)
    {
        return await getModelBase().GetModelWhere(where, param);
    }
    public static async Task<T> GetModelWhere(Expression<Func<T, bool>> where)
    {
        return await getModelBase().GetModelWhere(where);
    }
    /// <summary>
    /// 检查是否存在记录
    /// </summary>
    /// <param name="where"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    public static async Task<bool> Exists(string where, object param)
    {
        return await getModelBase().Exists(where, param);
    }
    public static async Task<bool> Exists(Expression<Func<T, bool>> where)
    {
        return await getModelBase().Exists(where);
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
        return getModelBase().GetModelList(where, param, top, orderby);
    }
    public static List2<T> GetModelList(Expression<Func<T, bool>> where, int top = int.MaxValue, Expression<Func<T, bool>> orderby = null)
    {
        return getModelBase().GetModelList(where, top, orderby);
    }
    public static async Task<DataTable> GetFieldList(string fields, string where, object param, int top = int.MaxValue, string orderby = null)
    {
        return await getModelBase().GetFieldList(fields, where, param, top, orderby);
    }
    public static async Task<DataTable> GetFieldList(Expression<Func<T, bool>> fields, Expression<Func<T, bool>> where, int top = int.MaxValue, Expression<Func<T, bool>> orderby = null)
    {
        return await getModelBase().GetFieldList(fields, where, top, orderby);
    }


    /// <summary>
    /// 获取分页对象
    /// </summary>
    /// <returns></returns>
    public static PagerEx<T> Pager(string where, object param, int pageindex, int pagesize, string orderby = null)
    {
        return getModelBase().Pager(where, param, pageindex, pagesize, orderby);
    }
    public static PagerEx<T> Pager(Expression<Func<T, bool>> where, int pageindex, int pagesize, Expression<Func<T, bool>> orderby = null)
    {
        return getModelBase().Pager(where, pageindex, pagesize, orderby);
    }
    /// <summary>
    /// 获取分页对象
    /// </summary>
    /// <returns></returns>
    public static PagerEx<T> Pager(string where, object param, int pageindex, int pagesize, string sort, SortBy order)
    {
        return getModelBase().Pager(where, param, pageindex, pagesize, sort, order);
    }
    public static PagerEx<T> Pager(Expression<Func<T, bool>> where, int pageindex, int pagesize, string sort, SortBy order)
    {
        return getModelBase().Pager(where, pageindex, pagesize, sort, order);
    }
}