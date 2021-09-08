using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


public class List2<T> where T : ModelBase<T>, new()
{
    private IConnectionProvider conn;
    private string fields = "*";
    private string tablename;
    private string where;
    private object param;
    private int top;
    private string orderby;
    //private string groupby;
    public List2(IConnectionProvider conn, string tablename, string where, object param, int top, string orderby)
    {
        this.conn = conn;
        this.tablename = tablename;
        this.where = where;
        this.param = param;
        this.top = top;
        this.orderby = orderby;
    }
    /* public List2(IConnectionProvider conn, string tablename, string where, object param, string groupby, int top)
        {
            this.conn = conn;
            this.tablename = tablename;
            this.where = where;
            this.param = param;
            this.top = top;
            this.groupby = groupby;
    } */

    public async Task<List<T>> GetList()
    {
        using (var connection = conn.GetDbConnection())
        {
            var result = await connection.QueryAsync<T>(@$"SELECT {fields} FROM ""{tablename}"" WHERE {where} ORDER BY {orderby} LIMIT {top}", param);
            return result.ToList();
        }
    }

    public async Task<int> GetCount()
    {
        using (var connection = conn.GetDbConnection())
        {
            var sql = @$"SELECT COUNT(*) FROM ""{tablename}"" WHERE {where} LIMIT {top}";
            var result = await connection.ExecuteScalarAsync<int>(sql, param);
            return result;
        }
    }
}