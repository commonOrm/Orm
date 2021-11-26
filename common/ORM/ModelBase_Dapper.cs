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

public class ModelBase_Dapper<T> : ModelBaseAbs<T>, IModelBase<T> where T : ModelBase<T>, new()
{

    public ModelBase_Dapper()
    {

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
    public async Task<bool> UpdateWhere(string set, string where, object param, SqlTranExtensions STE = null)
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
    public async Task<bool> UpdateWhere(Expression<Func<T, bool>> set, Expression<Func<T, bool>> where, SqlTranExtensions STE = null)
    {
        var setResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, set, sqlsign, 5000);
        var whereResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, where, sqlsign);
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
    public async Task<bool> DeleteWhere(string where, object param, SqlTranExtensions STE = null)
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
    public async Task<bool> DeleteWhere(Expression<Func<T, bool>> where, SqlTranExtensions STE = null)
    {
        var whereResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, where, sqlsign);
        return await DeleteWhere(whereResult.Lambda_Sql, LambdaToSQLFactory.ConvertToDictionary(whereResult.Lambda_SPArr), STE);
    }



    /// <summary>
    /// 获取一个对象 可能为null
    /// </summary>
    /// <param name="PrimaryKeyValue"></param>
    /// <returns></returns>
    public async Task<T> GetModel(object PrimaryKeyValue)
    {
        Dictionary<string, object> column_vals = new Dictionary<string, object>();
        column_vals.Add(getPrimaryKeyName(), PrimaryKeyValue);
        using (var connection = conn.GetDbConnection())
        {
            return await connection.QueryFirstOrDefaultAsync<T>(@$"SELECT * FROM ""{getTableName()}"" 
                        WHERE ""{getPrimaryKeyName()}""=@{getPrimaryKeyName()}", column_vals);
        }
    }

    public async Task<T> GetModelWhere(Expression<Func<T, bool>> where)
    {
        var whereResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, where, sqlsign);
        return await GetModelWhere(whereResult.Lambda_Sql, LambdaToSQLFactory.ConvertToDictionary(whereResult.Lambda_SPArr));
    }

    public List2<T> GetModelList(Expression<Func<T, bool>> where, int top = int.MaxValue, Expression<Func<T, bool>> orderby = null)
    {
        var whereResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, where, sqlsign);
        var orderbyResult = orderby == null ? null : LambdaToSQLFactory.Get<T>(SQLSort.SQLOrder, orderby, sqlsign);
        return GetModelList(whereResult.Lambda_Sql, LambdaToSQLFactory.ConvertToDictionary(whereResult.Lambda_SPArr), top, orderby == null ? null : orderbyResult.Lambda_Sql);
    }

    public async Task<bool> Exists(Expression<Func<T, bool>> where)
    {
        var whereResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, where, sqlsign);
        return await Exists(whereResult.Lambda_Sql, LambdaToSQLFactory.ConvertToDictionary(whereResult.Lambda_SPArr));
    }

    public async Task<DataTable> GetFieldList(Expression<Func<T, bool>> fields, Expression<Func<T, bool>> where, int top = int.MaxValue, Expression<Func<T, bool>> orderby = null)
    {
        var fieldsResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLFields, fields, sqlsign);
        var whereResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, where, sqlsign);
        var orderbyResult = orderby == null ? null : LambdaToSQLFactory.Get<T>(SQLSort.SQLOrder, orderby, sqlsign);
        return await GetFieldList(fieldsResult.Lambda_Sql, whereResult.Lambda_Sql, LambdaToSQLFactory.ConvertToDictionary(whereResult.Lambda_SPArr), top, orderby == null ? null : orderbyResult.Lambda_Sql);
    }

    public PagerEx<T> Pager(Expression<Func<T, bool>> where, int pageindex, int pagesize, Expression<Func<T, bool>> orderby = null)
    {
        var whereResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, where, sqlsign);
        var orderbyResult = orderby == null ? null : LambdaToSQLFactory.Get<T>(SQLSort.SQLOrder, orderby, sqlsign);
        return Pager(whereResult.Lambda_Sql, LambdaToSQLFactory.ConvertToDictionary(whereResult.Lambda_SPArr), pageindex, pagesize, orderby == null ? null : orderbyResult.Lambda_Sql);
    }

    public PagerEx<T> Pager(Expression<Func<T, bool>> where, int pageindex, int pagesize, string sort, SortBy order)
    {
        var whereResult = LambdaToSQLFactory.Get<T>(SQLSort.SQLWhere, where, sqlsign);
        string orderby = string.IsNullOrWhiteSpace(sort) ? null : @$"""{sort}"" {order}";

        return Pager(whereResult.Lambda_Sql, LambdaToSQLFactory.ConvertToDictionary(whereResult.Lambda_SPArr), pageindex, pagesize, orderby);
    }
}