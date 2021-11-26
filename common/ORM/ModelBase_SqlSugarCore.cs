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
    /// ����
    /// </summary>
    /// <returns></returns>
    public async Task<object> Add(SqlTranExtensions STE = null)
    {
        if (STE != null)
        {
            return await STE.db.Insertable<T>(model as T).ExecuteReturnIdentityAsync();
        }
        else
            using (var db = conn.GetSqlSugarClient())
            {
                object key = await db.Insertable<T>(model as T).ExecuteReturnIdentityAsync();
                return key;
            }
    }

    /// <summary>
    /// ����
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
    /// ���� ��������������
    /// </summary>
    /// <param name="set"></param>
    /// <param name="where"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    public async Task<bool> UpdateWhere(string set, string where, object param, SqlTranExtensions STE = null)
    {
        if (string.IsNullOrWhiteSpace(where))
            throw new MyException($"��UpdateWhere�� where ��������Ϊ��");

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
    /// ɾ��
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
    /// ɾ�� ��������������
    /// </summary>
    /// <param name="where"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    public async Task<bool> DeleteWhere(string where, object param, SqlTranExtensions STE = null)
    {
        if (string.IsNullOrWhiteSpace(where))
            throw new MyException($"��DeleteWhere�� where ��������Ϊ��");

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
    /// ��ȡһ������ ����Ϊnull
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

    public async Task<T> GetModelWhere(Expression<Func<T, bool>> where)
    {
        using (var db = conn.GetSqlSugarClient())
        {
            return await db.Queryable<T>().SingleAsync(where);
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