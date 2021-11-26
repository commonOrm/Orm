using common.ConnectionProvider;
using common.ORM;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

public class PagerEx<T> where T : ModelBase<T>, new()
{
    private ILogger<PagerEx<T>> logger;

    private IConnectionProvider conn;
    private SQLSign sqlsign;
    private string tablename;

    private string where;
    private object param;

    private string orderby;

    /// <summary>
    /// 从0开始 【超过页数后返回空即可】
    /// </summary>
    public int PageIndex;
    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize;
    /// <summary>
    /// 总记录数
    /// </summary>
    public int RecordCount;
    /// <summary>
    /// 总页数
    /// </summary>
    public int PageCount;

    public PagerEx(string tablename, string where, object param, int pageindex, int pagesize, string orderby)
    {
        this.logger = ServiceLocator.Instance.GetService(typeof(ILogger<PagerEx<T>>)) as ILogger<PagerEx<T>>;
        this.conn = ServiceLocator.Instance.GetService(typeof(IConnectionProvider)) as IConnectionProvider;
        this.sqlsign = SQLSign.Create(conn);
        this.tablename = tablename;
        this.where = where;
        this.param = param;
        this.PageIndex = Math.Max(0, pageindex);
        this.PageSize = Math.Max(1, pagesize);
        this.orderby = orderby;
    }

    public async Task<List<T>> GetDataList()
    {
        //总记录数
        RecordCount = await new List2<T>(conn, tablename, where, param, int.MaxValue, orderby).GetCount();

        //总页数
        PageCount = Math.Ceiling((decimal)RecordCount / (decimal)PageSize).ToInt32();

        //修正当前页的索引
        //PageIndex = Math.Min(PageIndex, PageCount - 1);
        //PageIndex = Math.Max(0, PageIndex);

        //从0开始 【超过页数后返回空即可】
        string sql = sqlsign.Create_GetPagerSQLEx(tablename, where, orderby, PageSize, PageIndex);
        try
        {
            if (conn is SqlSugarClientProvider)
                using (var db = conn.GetSqlSugarClient())
                {
                    return await db.Ado.SqlQueryAsync<T>(sql, param);
                }
            else
                using (var connection = conn.GetDbConnection())
                {
                    return (await connection.QueryAsync<T>(sql, param)).ToList();
                }
        }
        catch (Exception ce)
        {
            logger.LogError(ce, ce.Message + "#" + sql);
            throw ce;
        }
    }
}

