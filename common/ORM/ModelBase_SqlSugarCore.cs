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


public class ModelBase_SqlSugarCore<T> : ModelBaseAbs<T>, IModelBase<T> where T : ModelBase<T>, new()
{

    public ModelBase_SqlSugarCore()
    {
        
    }

    /// <summary>
    /// 新增
    /// </summary>
    /// <returns></returns>
    public async Task<object> Add(SqlTranExtensions STE = null)
    {
        if (STE != null)
        {
            return await STE.db.Insertable(model).ExecuteReturnIdentityAsync();
        }
        else
            using (var db = conn.GetSqlSugarClient())
            {
               var sql = db.Insertable<T>(model as T).ToSql();

               object key =  await db.Insertable<T>(model as T).ExecuteReturnIdentityAsync();
               return key;
            }
    }

    /// <summary>
    /// 更新
    /// </summary>
    /// <returns></returns>
    public async Task<bool> Update(SqlTranExtensions STE = null)
    {
        if (STE != null)
        {
            var result = await STE.db.Updateable(model).ExecuteCommandAsync();
            return result > 0;
        }
        else
            using (var db = conn.GetSqlSugarClient())
            {
                var result = await db.Updateable(model).ExecuteCommandAsync();
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
    public async Task<bool> UpdateWhere(string set, string where, object param, SqlTranExtensions STE = null)
    {
        if (string.IsNullOrWhiteSpace(where))
            throw new MyException($"【UpdateWhere】 where 参数不能为空");

        var sql = $@"UPDATE ""{getTableName()}"" SET {set} WHERE {where};";
        if (STE != null)
        {
            var result = await STE.db.Ado.ExecuteCommandAsync(sql, param);
            return result > 0;
        }
        else
            using (var db = conn.GetSqlSugarClient())
            {
                var result = await db.Ado.ExecuteCommandAsync(sql, param);
                return result > 0;
            }
    }
    public async Task<bool> UpdateWhere(Expression<Func<T, bool>> set, Expression<Func<T, bool>> where, SqlTranExtensions STE = null)
    {
        if (STE != null)
        {
            var result = await STE.db.Updateable<T>()
                                    .SetColumns(set)
                                    .Where(where)
                                    .ExecuteCommandAsync();
            return result > 0;
        }
        else
            using (var db = conn.GetSqlSugarClient())
            {
                var result = await db.Updateable<T>()
                                    .SetColumns(set)
                                    .Where(where)
                                    .ExecuteCommandAsync();
                return result > 0;
            }
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <returns></returns>
    public async Task<bool> Delete(SqlTranExtensions STE = null)
    {
        if (STE != null)
        {
            var result = await STE.db.Deleteable<T>().In(getPrimaryKeyValue()).ExecuteCommandAsync();
            return result > 0;
        }
        else
            using (var db = conn.GetSqlSugarClient())
            {
                var result = await db.Deleteable<T>().In(getPrimaryKeyValue()).ExecuteCommandAsync();
                return result > 0;
            }
    }

    /// <summary>
    /// 删除 按条件（多条）
    /// </summary>
    /// <param name="where"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    public async Task<bool> DeleteWhere(string where, object param, SqlTranExtensions STE = null)
    {
        if (string.IsNullOrWhiteSpace(where))
            throw new MyException($"【DeleteWhere】 where 参数不能为空");

        var sql = $@"DELETE FROM ""{getTableName()}"" WHERE {where} ;";
        if (STE != null)
        {
            var result = await STE.db.Ado.ExecuteCommandAsync(sql, param);
            return result > 0;
        }
        else
            using (var db = conn.GetSqlSugarClient())
            {
                var result = await db.Ado.ExecuteCommandAsync(sql, param);
                return result > 0;
            }
    }
    public async Task<bool> DeleteWhere(Expression<Func<T, bool>> where, SqlTranExtensions STE = null)
    {
        if (STE != null)
        {
            var result = await STE.db.Deleteable<T>().Where(where).ExecuteCommandAsync();
            return result > 0;
        }
        else
            using (var db = conn.GetSqlSugarClient())
            {
                var result = await db.Deleteable<T>().Where(where).ExecuteCommandAsync();
                return result > 0;
            }
    }

    /// <summary>
    /// 获取一个对象 可能为null
    /// </summary>
    /// <param name="PrimaryKeyValue"></param>
    /// <returns></returns>
    public async Task<T> GetModel(object PrimaryKeyValue)
    {
        using (var db = conn.GetSqlSugarClient())
        {
            return await db.Queryable<T>().InSingleAsync(getPrimaryKeyValue());
        }
    }
    /// <summary>
    /// 获取一个对象 可能为null
    /// </summary>
    /// <param name="where"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    public async Task<T> GetModelWhere(string where, object param)
    {
        if (string.IsNullOrWhiteSpace(where))
            where = "1=1";

        using (var db = conn.GetSqlSugarClient())
        {
            var dataList = await db.Queryable<T>().Where(where, param).ToListAsync();
            return dataList.Count > 0 ? dataList[0] : null;
        }
    }
    public async Task<T> GetModelWhere(Expression<Func<T, bool>> where)
    {
        using (var db = conn.GetSqlSugarClient())
        {
            return await db.Queryable<T>().SingleAsync(where);
        }
    }
    /// <summary>
    /// 检查是否存在记录
    /// </summary>
    /// <param name="where"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    public async Task<bool> Exists(string where, object param)
    {
        if (string.IsNullOrWhiteSpace(where))
            where = "1=1";

        using (var db = conn.GetSqlSugarClient())
        {
            var count = await db.Ado.GetIntAsync(@$"SELECT COUNT(1) FROM ""{getTableName()}"" WHERE {where}", param);
            return count > 0;
        }
    }
    public async Task<bool> Exists(Expression<Func<T, bool>> where)
    {
        using (var db = conn.GetSqlSugarClient())
        {
            return await db.Queryable<T>().Where(where).AnyAsync();
        }
    }
    /// <summary>
    /// 获取一个对象集合
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
    public List2<T> GetModelList(Expression<Func<T, bool>> where, int top = int.MaxValue, Expression<Func<T, bool>> orderby = null)
    {
        var whereResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, where, sqlsign);
        var orderbyResult = orderby == null ? null : LambdaToSQLFactory.Get<T>(SQLSort.SQLOrder, orderby, sqlsign);
        return GetModelList(whereResult.Lambda_Sql, LambdaToSQLFactory.ConvertToDictionary(whereResult.Lambda_SPArr), top, orderby == null ? null : orderbyResult.Lambda_Sql);
    }
    public async Task<DataTable> GetFieldList(string fields, string where, object param, int top = int.MaxValue, string orderby = null)
    {
        if (string.IsNullOrWhiteSpace(fields))
            fields = "*";

        if (string.IsNullOrWhiteSpace(where))
            where = "1=1";

        orderby = orderby ?? @$" ""{getPrimaryKeyName()}"" ASC ";

        //return new List2<T>(conn, getTableName(), where, param, top, orderby);
        using (var db = conn.GetSqlSugarClient())
        {
            var tablename = getTableName();
            DataTable table = new DataTable("MyTable");
            var sql = sqlsign.Create_GetListSQLEx(fields, tablename, where, orderby, top);
            return await db.Ado.GetDataTableAsync(sql, param);
        }
    }
    public async Task<DataTable> GetFieldList(Expression<Func<T, bool>> fields, Expression<Func<T, bool>> where, int top = int.MaxValue, Expression<Func<T, bool>> orderby = null)
    {
        var fieldsResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLFields, fields, sqlsign);
        var whereResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, where, sqlsign);
        var orderbyResult = orderby == null ? null : LambdaToSQLFactory.Get<T>(SQLSort.SQLOrder, orderby, sqlsign);
        return await GetFieldList(fieldsResult.Lambda_Sql, whereResult.Lambda_Sql, LambdaToSQLFactory.ConvertToDictionary(whereResult.Lambda_SPArr), top, orderby == null ? null : orderbyResult.Lambda_Sql);
    }

    /// <summary>
    /// 获取分页对象
    /// </summary>
    /// <returns></returns>
    public PagerEx<T> Pager(string where, object param, int pageindex, int pagesize, string orderby = null)
    {
        if (string.IsNullOrWhiteSpace(where))
            where = "1=1";

        orderby = orderby ?? @$" ""{getPrimaryKeyName()}"" ASC ";
        return new PagerEx<T>(getTableName(), where, param, pageindex, pagesize, orderby);
    }
    public PagerEx<T> Pager(Expression<Func<T, bool>> where, int pageindex, int pagesize, Expression<Func<T, bool>> orderby = null)
    {
        var whereResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, where, sqlsign);
        var orderbyResult = orderby == null ? null : LambdaToSQLFactory.Get<T>(SQLSort.SQLOrder, orderby, sqlsign);
        return Pager(whereResult.Lambda_Sql, LambdaToSQLFactory.ConvertToDictionary(whereResult.Lambda_SPArr), pageindex, pagesize, orderby == null ? null : orderbyResult.Lambda_Sql);
    }
    /// <summary>
    /// 获取分页对象
    /// </summary>
    /// <returns></returns>
    public PagerEx<T> Pager(string where, object param, int pageindex, int pagesize, string sort, SortBy order)
    {
        string orderby = string.IsNullOrWhiteSpace(sort) ? null : @$"""{sort}"" {order}";

        return Pager(where, param, pageindex, pagesize, orderby);
    }
    public PagerEx<T> Pager(Expression<Func<T, bool>> where, int pageindex, int pagesize, string sort, SortBy order)
    {
        var whereResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, where, sqlsign);
        string orderby = string.IsNullOrWhiteSpace(sort) ? null : @$"""{sort}"" {order}";

        return Pager(whereResult.Lambda_Sql, LambdaToSQLFactory.ConvertToDictionary(whereResult.Lambda_SPArr), pageindex, pagesize, orderby);
    }
}