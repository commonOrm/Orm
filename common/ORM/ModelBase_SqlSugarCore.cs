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
    /// <returns>自增ID 或 True/False(主键不自增的情况)</returns>
    public async Task<object> Add(SqlTranExtensions STE = null)
    {
        var primaryKey = getPrimaryKeyColumn();
        if (STE != null)
        {
            if (primaryKey.GetType() == typeof(int))
                return await STE.db.Insertable<T>(model as T).ExecuteReturnIdentityAsync();
            else
                return await STE.db.Insertable<T>(model as T).ExecuteCommandAsync();
        }
        else
            using (var db = conn.GetSqlSugarClient())
            {
                if (primaryKey.GetType() == typeof(int))
                    return await db.Insertable<T>(model as T).ExecuteReturnIdentityAsync();
                else
                    return await db.Insertable<T>(model as T).ExecuteCommandAsync();
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
            var result = await STE.db.Updateable<T>(model as T).ExecuteCommandAsync();
            return result > 0;
        }
        else
            using (var db = conn.GetSqlSugarClient())
            {
                var result = await db.Updateable<T>(model as T).ExecuteCommandAsync();
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
            return await db.Queryable<T>().InSingleAsync(PrimaryKeyValue);
        }
    }

    public async Task<T> GetModelWhere(Expression<Func<T, bool>> where)
    {
        using (var db = conn.GetSqlSugarClient())
        {
            return await db.Queryable<T>().Where(where).FirstAsync();
        }
    }
    public List2<T> GetModelList(Expression<Func<T, bool>> where, int top = int.MaxValue, Expression<Func<T, bool>> orderby = null)
    {
        var orderbyResult = orderby == null ? null : LambdaToSQLFactory.Get<T>(SQLSort.SQLOrder, orderby, sqlsign);
        var orderbyStr = orderbyResult == null ? @$" ""{getPrimaryKeyName()}"" ASC " : orderbyResult.Lambda_Sql;

        using (var db = conn.GetSqlSugarClient())
        {
            var sqlParams = db.Queryable<T>().Where(where).ToSql();

            var sql = sqlParams.Key.Split("WHERE")[1];
            var param = sqlParams.Value;

            return new List2<T>(conn, getTableName(), sql, param, top, orderbyStr);
        }
    }

    public async Task<bool> Exists(Expression<Func<T, bool>> where)
    {
        using (var db = conn.GetSqlSugarClient())
        {
            return await db.Queryable<T>().Where(where).AnyAsync();
        }
    }

    public async Task<DataTable> GetFieldList(Expression<Func<T, bool>> fields, Expression<Func<T, bool>> where, int top = int.MaxValue, Expression<Func<T, bool>> orderby = null)
    {
        var fieldsResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLFields, fields, sqlsign);
        var orderbyResult = orderby == null ? null : LambdaToSQLFactory.Get<T>(SQLSort.SQLOrder, orderby, sqlsign);
        var orderbyStr = orderbyResult == null ? @$" ""{getPrimaryKeyName()}"" ASC " : orderbyResult.Lambda_Sql;

        using (var db = conn.GetSqlSugarClient())
        {
            var sqlParams = db.Queryable<T>().Where(where).ToSql();

            var sql = sqlParams.Key.Split("WHERE")[1];
            var param = sqlParams.Value;

            return await new List2<T>(conn, getTableName(), sql, param, top, orderbyStr, fieldsResult.Lambda_Sql).GetDataTable();
        }
    }

    public PagerEx<T> Pager(Expression<Func<T, bool>> where, int pageindex, int pagesize, Expression<Func<T, bool>> orderby = null)
    {
        //var whereResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, where, sqlsign);
        var orderbyResult = orderby == null ? null : LambdaToSQLFactory.Get<T>(SQLSort.SQLOrder, orderby, sqlsign);
        var orderbyStr = orderbyResult == null ? @$" ""{getPrimaryKeyName()}"" ASC " : orderbyResult.Lambda_Sql;

        using (var db = conn.GetSqlSugarClient())
        {
            var sqlParams = db.Queryable<T>().Where(where).ToSql();
            var sql = sqlParams.Key.Split("WHERE")[1];
            var param = sqlParams.Value;

            return new PagerEx<T>(getTableName(), sql, param, pageindex, pagesize, orderbyStr);
        }
    }

    public PagerEx<T> Pager(Expression<Func<T, bool>> where, int pageindex, int pagesize, string sort, SortBy order)
    {
        //var whereResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, where, sqlsign);
        string orderby = string.IsNullOrWhiteSpace(sort) ? null : @$"""{sort}"" {order}";

        using (var db = conn.GetSqlSugarClient())
        {
            var sqlParams = db.Queryable<T>().Where(where).ToSql();
            var sql = sqlParams.Key.Split("WHERE")[1];
            var param = sqlParams.Value;

            return new PagerEx<T>(getTableName(), sql, param, pageindex, pagesize, orderby);
        }
    }
}