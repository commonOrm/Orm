using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class PagerEx<T> where T : ModelBase<T>, new()
{
    private IConnectionProvider conn;
    private string tablename;
    private string where;
    private object param;
    private string orderby;

    public int PageIndex;
    public int PageSize;
    public int RecordCount;
    public int PageCount;

    public PagerEx(string tablename, string where, object param, int pageindex, int pagesize, string orderby)
    {
        conn = ServiceLocator.Instance.GetService(typeof(IConnectionProvider)) as IConnectionProvider;
        this.tablename = tablename;
        this.where = where;
        this.param = param;
        this.PageIndex = Math.Max(0, pageindex);
        this.PageSize = Math.Max(1, pagesize); ;
        this.orderby = orderby;
    }

    public async Task<List<T>> GetDataList()
    {
        using (var connection = conn.GetDbConnection())
        {
            //总记录数
            RecordCount = await new List2<T>(conn, tablename, where, param, int.MaxValue, orderby).GetCount();

            //总页数
            PageCount = Math.Ceiling((decimal)RecordCount / (decimal)PageSize).ToInt32();

            //修正当前页的索引
            //PageIndex = Math.Min(PageIndex, PageCount - 1);
            //PageIndex = Math.Max(0, PageIndex);

            //从0开始 【超过页数后返回空即可】
            return connection.QueryAsync<T>(@$"SELECT * FROM ""{tablename}"" 
                            WHERE {where} 
                            ORDER BY {orderby} 
                            LIMIT {PageSize} OFFSET {PageIndex * PageSize}",
                param)
                .Result.ToList();
        }
    }
}

