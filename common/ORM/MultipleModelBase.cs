using common.ConnectionProvider;
using common.ORM;
using common.ORM.LambdaToSQL;
using Dapper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

/*
select * from PlaceChina 
select * from PlaceChinaPlay 
--在表中存在至少一个匹配时，INNER JOIN 关键字返回行。
SELECT  PlaceChinaPlay.id
FROM PlaceChina 
JOIN PlaceChinaPlay
ON PlaceChina.id = PlaceChinaPlay.PlaceChinaID 
where 1 = 1 GROUP BY PlaceChinaPlay.id HAVING PlaceChinaPlay.id = '1' order by PlaceChinaPlay.id desc 
--LEFT JOIN 关键字会从左表 (table_name1) 那里返回所有的行，即使在右表 (table_name2) 中没有匹配的行。
SELECT *
FROM PlaceChina
LEFT JOIN PlaceChinaPlay
ON PlaceChina.id = PlaceChinaPlay.PlaceChinaID 
--RIGHT JOIN 关键字会右表 (table_name2) 那里返回所有的行，即使在左表 (table_name1) 中没有匹配的行。
SELECT *
FROM PlaceChina
RIGHT JOIN PlaceChinaPlay
ON PlaceChina.id = PlaceChinaPlay.PlaceChinaID 
--只要其中某个表存在匹配，FULL JOIN 关键字就会返回行。
SELECT *
FROM PlaceChina
FULL  JOIN PlaceChinaPlay
ON PlaceChina.id = PlaceChinaPlay.PlaceChinaID 
   */

public enum JoinStyle
{
    INNER_JOIN, LEFT_JOIN, RIGHT_JOIN, FULL_JOIN
}


//执行顺序 ：from >  on > where > Group by > having > select > DISTINCT > order by > TOP

public abstract class DALBase<TReturn>
    where TReturn : class, new()
{
    protected JoinStyle[] _join;
    protected List<String> TName = new List<string>();
    private List<String> TableName = new List<string>();

    protected int _pagesize = 20;
    protected int _pageindex = -1;
    public int PageIndex { get { return _pageindex; } }
    protected int _recordcount = 0;
    public int RecordCount { get { return _recordcount; } }
    protected int _pagecount = 0;
    public int PageCount { get { return _pagecount; } }
    protected List<TReturn> _returnData = new List<TReturn>();
    public List<TReturn> ReturnData { get { return _returnData; } }

    public IConnectionProvider conn { get; set; }
    public SQLSign sqlsign { get; set; }

    public DALBase(JoinStyle[] join)
    {
        _join = join;

        if (ServiceLocator.Instance == null) throw new MyException("ServiceLocator.Instance is Null");
        if (conn == null)
        {
            if (ServiceLocator.Instance == null) throw new MyException("ServiceLocator.Instance is Null");
            conn = ServiceLocator.Instance.GetService(typeof(IConnectionProvider)) as IConnectionProvider;

            sqlsign = SQLSign.Create(conn);
        }
    }

    #region 通过筛选出来的集合进行统计计算，会涉及到另外一个新表

    /// <summary>
    /// 附件一个新的表 格式 INNER JOIN OrderTraveler as t* ON ( t.[OrderNumber] = t*.[OrderNumber] )    *是一个数字，根据这个表是第几个给出值比如如果是第4个就给4
    /// </summary>
    public string AttachTable = "";
    /// <summary>
    /// 附件一个新的表 需要输入的列 一般是用来做统计 count(t4.id) as PeopleCount
    /// </summary>
    public List<string> AttachColumns = new List<string>();

    #endregion

    protected void AddTableName(string Name)
    {
        TableName.Add(sqlsign.Create_TableNameEx(Name));
    }

    protected void AddTName(ReadOnlyCollection<ParameterExpression> Parameters)
    {
        foreach (var _T in Parameters)
            if (!TName.Contains(_T.Name)) TName.Add(_T.Name);
    }

    public async Task GetList(LambdaToSQLPlus columns_SP, LambdaToSQLPlus[] on_SP_Arr, List<LambdaToSQLPlus[]> where_SP, LambdaToSQLPlus groupby_SP, LambdaToSQLPlus having_SP, LambdaToSQLPlus orderby_SP)
    {
        //int index = 0;
        List<SqlParameter> SqlParameterS = new List<SqlParameter>();
        List<string[]> whereS = new List<string[]>();
        //DealSQLAndParameter(columns_SP, ref index, ref SqlParameterS);
        if (columns_SP != null)
            SqlParameterS.AddRange(columns_SP.Lambda_SPArr);
        if (on_SP_Arr != null)
            foreach (var on_SP in on_SP_Arr)
            {
                //DealSQLAndParameter(on_SP, ref index, ref SqlParameterS);
                SqlParameterS.AddRange(on_SP.Lambda_SPArr);
            }
        //DealSQLAndParameter(where_SP, ref index, ref SqlParameterS);
        foreach (var w in where_SP)
        {
            List<string> whereS2 = new List<string>();
            foreach (var w2 in w)
            {
                whereS2.Add(w2.Lambda_Sql);
                SqlParameterS.AddRange(w2.Lambda_SPArr);
            }
            whereS.Add(whereS2.ToArray());
        }
        //DealSQLAndParameter(groupby_SP, ref index, ref SqlParameterS);
        if (groupby_SP != null)
            SqlParameterS.AddRange(groupby_SP.Lambda_SPArr);
        //DealSQLAndParameter(having_SP, ref index, ref SqlParameterS);
        if (having_SP != null)
            SqlParameterS.AddRange(having_SP.Lambda_SPArr);
        //DealSQLAndParameter(orderby_SP, ref index, ref SqlParameterS);
        if (orderby_SP != null)
            SqlParameterS.AddRange(orderby_SP.Lambda_SPArr);

        List<string> whereS_2 = new List<string>();
        foreach (var w in whereS)
        {
            if (w.Length > 1)
                whereS_2.Add(" ( " + string.Join(" OR ", w) + " ) ");
            else if (w.Length == 1)
                whereS_2.Add(w[0]);

        }
        string Lambda_Sql = string.Join(" AND ", whereS_2.ToArray());
        string SQL = "";

        List<string> TrySQL = new List<string>();
        var param = LambdaToSQLFactory.ConvertToDictionary(SqlParameterS.ToArray());

        if (_pageindex > -1)
        {
            //分页
            string SQLCount = "";
            SQLCount += string.Format("SELECT {0} ", groupby_SP != null ? (columns_SP == null ? "*" : columns_SP.Lambda_Sql) : "COUNT(*)") +
                        string.Format("FROM {0} as {1} ", TableName[0], TName[0]);
            for (int i = 1; i < TName.Count; i++)
            {
                SQLCount += string.Format("{0} {1} as {2} ", _join[i - 1].ToString2().Replace("_", " "), TableName[i], TName[i]);
                SQLCount += string.Format("ON {0}  ", on_SP_Arr[i - 1].Lambda_Sql);
            }

            SQLCount += string.Format("WHERE {0} ", Lambda_Sql) +
                                     (groupby_SP == null ? "" : string.Format("GROUP BY {0} HAVING {1} ", groupby_SP.Lambda_Sql, having_SP.Lambda_Sql));

            if (groupby_SP != null)
                SQLCount = "SELECT COUNT(*) from ( " + SQLCount + " ) tab_2";

            if (conn is SqlSugarClientProvider)
                using (var db = conn.GetSqlSugarClient())
                {
                    var dt = await db.Ado.GetDataTableAsync(SQLCount.ToString(), param);
                    if (dt.Rows.Count > 0)
                        _recordcount = dt.Rows[0][0].ToInt32();
                    else
                        _recordcount = 0;
                }
            else
                using (var connection = conn.GetDbConnection())
                {
                    var dr = await connection.ExecuteReaderAsync(SQLCount.ToString(), param);
                    if (dr.Read())
                        _recordcount = int.Parse(dr[0].ToString());
                    else
                        _recordcount = 0;
                }

            if ((_recordcount % _pagesize) > 0) { _pagecount = _recordcount / _pagesize + 1; } else { _pagecount = _recordcount / _pagesize; }
            if (_pageindex < 0) { _pageindex = 0; } //【超过页数后返回空即可】

            /*SQL2005以上支持 ROW_NUMBER() OVER()  分页方式*/
            //SQL += string.Format("SELECT TOP {0} {1} ", _pagesize, (columns_SP == null ? "*" : columns_SP.SQL)) +
            //       "FROM (" +
            //            string.Format("SELECT ROW_NUMBER() OVER (ORDER BY {0}) AS RowNumber, {1} ", orderby_SP.SQL, columns_SP.SQL) +
            //            string.Format("FROM {0} as {1} ", TableName, TName[0]) +
            //            string.Format("{0} {1} as {2} ", _join.ToString2().Replace("_", " "), TableName2, TName[1]) +
            //            string.Format("ON {0}  ", on_SP.SQL) +
            //            string.Format("WHERE {0} ", where_SP.SQL) +
            //            (groupby_SP == null ? "" : string.Format("GROUP BY {0} HAVING {1} ", groupby_SP.SQL, having_SP.SQL)) +
            //       ") as A  " +
            //       string.Format("WHERE RowNumber > {0}*({1}-1) ", _pagesize, _pageindex);
            /*SQL 2012支持的OFFSET/FETCH NEXT分页方式*/



            SQL += string.Format("SELECT {0} ", (columns_SP == null ? "*" : (columns_SP.Lambda_Sql + (AttachColumns.Count > 0 ? ("," + string.Join(",", AttachColumns.ToArray())) : "")))) +
                        string.Format("FROM {0} as {1} ", TableName[0], TName[0]);
            for (int i = 1; i < TName.Count; i++)
            {
                SQL += string.Format("{0} {1} as {2} ", _join[i - 1].ToString2().Replace("_", " "), TableName[i], TName[i]);
                SQL += string.Format("ON {0}  ", on_SP_Arr[i - 1].Lambda_Sql);
            }

            //加入附加表
            if (!string.IsNullOrWhiteSpace(AttachTable)) SQL += AttachTable;

            SQL += string.Format("WHERE {0} ", Lambda_Sql) +
               (groupby_SP == null ? "" : string.Format("GROUP BY {0} HAVING {1} ", groupby_SP.Lambda_Sql, having_SP.Lambda_Sql)) +
               (orderby_SP == null ? "" : string.Format("ORDER BY {0} ", orderby_SP.Lambda_Sql));

            SQL += sqlsign.Create_GetMultiplePagerSQLEx(_pagesize, _pageindex);
        }
        else
        {
            //不分页
            SQL += string.Format("SELECT {0} ", (columns_SP == null ? "*" : (columns_SP.Lambda_Sql + (AttachColumns.Count > 0 ? ("," + string.Join(",", AttachColumns.ToArray())) : "")))) +
                        string.Format("FROM {0} as {1} ", TableName[0], TName[0]);
            for (int i = 1; i < TName.Count; i++)
            {
                SQL += string.Format("{0} {1} as {2} ", _join[i - 1].ToString2().Replace("_", " "), TableName[i], TName[i]);
                SQL += string.Format("ON {0}  ", on_SP_Arr[i - 1].Lambda_Sql);
            }

            //加入附加表
            if (!string.IsNullOrWhiteSpace(AttachTable)) SQL += AttachTable;

            SQL += string.Format("WHERE {0} ", Lambda_Sql) +
                (groupby_SP == null ? "" : string.Format("GROUP BY {0} HAVING {1} ", groupby_SP.Lambda_Sql, having_SP.Lambda_Sql)) +
                (orderby_SP == null ? "" : string.Format("ORDER BY {0} ", orderby_SP.Lambda_Sql));
        }

        if (conn is SqlSugarClientProvider)
            using (var db = conn.GetSqlSugarClient())
            {
                var result = await db.Ado.SqlQueryAsync<TReturn>(SQL.ToString(), param);
                _returnData = result;
            }
        else
            using (var connection = conn.GetDbConnection())
            {
                var result = await connection.QueryAsync<TReturn>(SQL.ToString(), param);
                _returnData = result.ToList();
            }
    }

}

public class DAL<T, T2, TReturn> : DALBase<TReturn>
    where T : ModelBase<T>, new()
    where T2 : ModelBase<T2>, new()
    where TReturn : class, new()
{
    private System.Linq.Expressions.Expression<Func<T, T2, bool>> _columns;
    private System.Linq.Expressions.Expression<Func<T, T2, bool>> _on;
    private List<System.Linq.Expressions.Expression<Func<T, T2, bool>>[]> _where;
    private System.Linq.Expressions.Expression<Func<T, T2, bool>> _groupby;
    private System.Linq.Expressions.Expression<Func<T, T2, bool>> _having;
    private System.Linq.Expressions.Expression<Func<T, T2, bool>> _orderby;

    public DAL(JoinStyle[] join)
        : base(join)
    { }

    public DAL<T, T2, TReturn> SetPageSize(int pagesize)
    {
        _pagesize = pagesize; return this;
    }
    public DAL<T, T2, TReturn> SetPageIndex(int pageindex)
    {
        _pageindex = pageindex; return this;
    }
    public DAL<T, T2, TReturn> Columns(System.Linq.Expressions.Expression<Func<T, T2, bool>> columns)
    {
        _columns = columns; return this;
    }
    public DAL<T, T2, TReturn> On(System.Linq.Expressions.Expression<Func<T, T2, bool>> on)
    {
        _on = on; return this;
    }
    public DAL<T, T2, TReturn> Where(System.Linq.Expressions.Expression<Func<T, T2, bool>> where)
    {
        return Where(new Expression<Func<T, T2, bool>>[] { where });
    }
    public DAL<T, T2, TReturn> Where(System.Linq.Expressions.Expression<Func<T, T2, bool>>[] wheres_or)
    {
        if (_where == null)
            _where = new List<Expression<Func<T, T2, bool>>[]>();

        _where.Add(wheres_or);

        foreach (var w in wheres_or)
            base.AddTName(w.Parameters);

        return this;
    }

    public DAL<T, T2, TReturn> GroupBy(System.Linq.Expressions.Expression<Func<T, T2, bool>> groupby)
    {
        _groupby = groupby; return this;
    }
    public DAL<T, T2, TReturn> Having(System.Linq.Expressions.Expression<Func<T, T2, bool>> having)
    {
        _having = having; return this;
    }
    public DAL<T, T2, TReturn> OrderBy(System.Linq.Expressions.Expression<Func<T, T2, bool>> orderby)
    {
        _orderby = orderby; return this;
    }

    public async Task GetList()
    {
        AddTableName(typeof(T).Name.ToString2());
        AddTableName(typeof(T2).Name.ToString2());

        LambdaToSQLPlus on_SP = null;
        if (_on != null) on_SP = LambdaToSQLFactory.Get<T, T2>(SQLSort.SQLWhere, _on, sqlsign);
        LambdaToSQLPlus columns_SP = null;
        if (_columns != null) columns_SP = LambdaToSQLFactory.Get<T, T2>(SQLSort.SQLFields, _columns, sqlsign);
        List<LambdaToSQLPlus[]> where_SP = new List<LambdaToSQLPlus[]>();
        int sign = 7000;
        foreach (var w in _where)
        {
            List<LambdaToSQLPlus> where_SP2 = new List<LambdaToSQLPlus>();
            foreach (var w2 in w)
            {
                where_SP2.Add(LambdaToSQLFactory.Get<T, T2>(SQLSort.SQLWhere, w2, sqlsign, sign)); sign += 100;
            }
            where_SP.Add(where_SP2.ToArray());
        }
        LambdaToSQLPlus groupby_SP = null;
        if (_groupby != null) groupby_SP = LambdaToSQLFactory.Get<T, T2>(SQLSort.SQLFields, _groupby, sqlsign);
        LambdaToSQLPlus having_SP = null;
        if (_having != null) having_SP = LambdaToSQLFactory.Get<T, T2>(SQLSort.SQLWhere, _having, sqlsign, 8000);
        LambdaToSQLPlus orderby_SP = null;
        if (_orderby != null) orderby_SP = LambdaToSQLFactory.Get<T, T2>(SQLSort.SQLOrder, _orderby, sqlsign);

        await base.GetList(columns_SP, new LambdaToSQLPlus[] { on_SP }, where_SP, groupby_SP, having_SP, orderby_SP);
    }

}

public class DAL<T, T2, T3, TReturn> : DALBase<TReturn>
    where T : ModelBase<T>, new()
    where T2 : ModelBase<T2>, new()
    where T3 : ModelBase<T3>, new()
    where TReturn : class, new()
{
    private System.Linq.Expressions.Expression<Func<T, T2, T3, bool>> _columns;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, bool>> _on;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, bool>> _on2;
    private List<System.Linq.Expressions.Expression<Func<T, T2, T3, bool>>[]> _where;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, bool>> _groupby;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, bool>> _having;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, bool>> _orderby;

    public DAL(JoinStyle[] join)
        : base(join)
    { }

    public DAL<T, T2, T3, TReturn> SetPageSize(int pagesize)
    {
        _pagesize = pagesize; return this;
    }
    /// <summary>
    /// -1 表示不分页
    /// </summary>
    /// <param name="pageindex"></param>
    /// <returns></returns>
    public DAL<T, T2, T3, TReturn> SetPageIndex(int pageindex)
    {
        _pageindex = pageindex; return this;
    }
    public DAL<T, T2, T3, TReturn> Columns(System.Linq.Expressions.Expression<Func<T, T2, T3, bool>> columns)
    {
        _columns = columns; return this;
    }
    public DAL<T, T2, T3, TReturn> On(System.Linq.Expressions.Expression<Func<T, T2, T3, bool>> on, System.Linq.Expressions.Expression<Func<T, T2, T3, bool>> on2)
    {
        _on = on; _on2 = on2; return this;
    }

    public System.Linq.Expressions.Expression<Func<T, T2, T3, bool>> CreateWhere(System.Linq.Expressions.Expression<Func<T, T2, T3, bool>> where)
    {
        return where;
    }
    public DAL<T, T2, T3, TReturn> Where(System.Linq.Expressions.Expression<Func<T, T2, T3, bool>> where)
    {
        return Wheres(new Expression<Func<T, T2, T3, bool>>[] { where });
    }
    public DAL<T, T2, T3, TReturn> Wheres(object[] _wheres_or)
    {
        List<System.Linq.Expressions.Expression<Func<T, T2, T3, bool>>> wheres_or = new List<Expression<Func<T, T2, T3, bool>>>();
        foreach (var obj in _wheres_or)
        {
            wheres_or.Add(obj as System.Linq.Expressions.Expression<Func<T, T2, T3, bool>>);
        }
        if (_where == null)
            _where = new List<Expression<Func<T, T2, T3, bool>>[]>();

        _where.Add(wheres_or.ToArray());

        foreach (var w in wheres_or)
            base.AddTName(w.Parameters);

        return this;
    }
    public DAL<T, T2, T3, TReturn> GroupBy(System.Linq.Expressions.Expression<Func<T, T2, T3, bool>> groupby)
    {
        _groupby = groupby; return this;
    }
    public DAL<T, T2, T3, TReturn> Having(System.Linq.Expressions.Expression<Func<T, T2, T3, bool>> having)
    {
        _having = having; return this;
    }
    public DAL<T, T2, T3, TReturn> OrderBy(System.Linq.Expressions.Expression<Func<T, T2, T3, bool>> orderby)
    {
        _orderby = orderby; return this;
    }

    public async Task GetList()
    {
        AddTableName(typeof(T).Name.ToString2());
        AddTableName(typeof(T2).Name.ToString2());
        AddTableName(typeof(T3).Name.ToString2());

        LambdaToSQLPlus on_SP = null;
        if (_on != null) on_SP = LambdaToSQLFactory.Get<T, T2, T3>(SQLSort.SQLWhere, _on, sqlsign);
        LambdaToSQLPlus on_SP2 = null;
        if (_on2 != null) on_SP2 = LambdaToSQLFactory.Get<T, T2, T3>(SQLSort.SQLWhere, _on2, sqlsign);

        LambdaToSQLPlus columns_SP = null;
        if (_columns != null) columns_SP = LambdaToSQLFactory.Get<T, T2, T3>(SQLSort.SQLFields, _columns, sqlsign);

        List<LambdaToSQLPlus[]> where_SP = new List<LambdaToSQLPlus[]>();
        int sign = 7000;
        foreach (var w in _where)
        {
            List<LambdaToSQLPlus> where_SP2 = new List<LambdaToSQLPlus>();
            foreach (var w2 in w)
            {
                where_SP2.Add(LambdaToSQLFactory.Get<T, T2, T3>(SQLSort.SQLWhere, w2, sqlsign, sign)); sign += 100;
            }
            where_SP.Add(where_SP2.ToArray());
        }

        LambdaToSQLPlus groupby_SP = null;
        if (_groupby != null) groupby_SP = LambdaToSQLFactory.Get<T, T2, T3>(SQLSort.SQLFields, _groupby, sqlsign);
        LambdaToSQLPlus having_SP = null;
        if (_having != null) having_SP = LambdaToSQLFactory.Get<T, T2, T3>(SQLSort.SQLWhere, _having, sqlsign, 8000);
        LambdaToSQLPlus orderby_SP = null;
        if (_orderby != null) orderby_SP = LambdaToSQLFactory.Get<T, T2, T3>(SQLSort.SQLOrder, _orderby, sqlsign);

        await base.GetList(columns_SP, new LambdaToSQLPlus[] { on_SP, on_SP2 }, where_SP, groupby_SP, having_SP, orderby_SP);
    }

}

public class DAL<T, T2, T3, T4, TReturn> : DALBase<TReturn>
    where T : ModelBase<T>, new()
    where T2 : ModelBase<T2>, new()
    where T3 : ModelBase<T3>, new()
    where T4 : ModelBase<T4>, new()
    where TReturn : class, new()
{
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, bool>> _columns;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, bool>> _on;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, bool>> _on2;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, bool>> _on3;
    private List<System.Linq.Expressions.Expression<Func<T, T2, T3, T4, bool>>[]> _where;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, bool>> _groupby;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, bool>> _having;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, bool>> _orderby;

    public DAL(JoinStyle[] join)
        : base(join)
    { }

    public DAL<T, T2, T3, T4, TReturn> SetPageSize(int pagesize)
    {
        _pagesize = pagesize; return this;
    }
    public DAL<T, T2, T3, T4, TReturn> SetPageIndex(int pageindex)
    {
        _pageindex = pageindex; return this;
    }
    public DAL<T, T2, T3, T4, TReturn> Columns(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, bool>> columns)
    {
        _columns = columns; return this;
    }
    public DAL<T, T2, T3, T4, TReturn> On(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, bool>> on, System.Linq.Expressions.Expression<Func<T, T2, T3, T4, bool>> on2, System.Linq.Expressions.Expression<Func<T, T2, T3, T4, bool>> on3)
    {
        _on = on; _on2 = on2; _on3 = on3; return this;
    }
    public DAL<T, T2, T3, T4, TReturn> Where(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, bool>> where)
    {
        return Where(new Expression<Func<T, T2, T3, T4, bool>>[] { where });
    }
    public DAL<T, T2, T3, T4, TReturn> Where(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, bool>>[] wheres_or)
    {
        if (_where == null)
            _where = new List<Expression<Func<T, T2, T3, T4, bool>>[]>();

        _where.Add(wheres_or);

        foreach (var w in wheres_or)
            base.AddTName(w.Parameters);

        return this;
    }

    public DAL<T, T2, T3, T4, TReturn> GroupBy(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, bool>> groupby)
    {
        _groupby = groupby; return this;
    }
    public DAL<T, T2, T3, T4, TReturn> Having(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, bool>> having)
    {
        _having = having; return this;
    }
    public DAL<T, T2, T3, T4, TReturn> OrderBy(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, bool>> orderby)
    {
        _orderby = orderby; return this;
    }

    public async Task GetList()
    {
        AddTableName(typeof(T).Name.ToString2());
        AddTableName(typeof(T2).Name.ToString2());
        AddTableName(typeof(T3).Name.ToString2());
        AddTableName(typeof(T4).Name.ToString2());

        LambdaToSQLPlus on_SP = null;
        if (_on != null) on_SP = LambdaToSQLFactory.Get<T, T2, T3, T4>(SQLSort.SQLWhere, _on, sqlsign);
        LambdaToSQLPlus on_SP2 = null;
        if (_on2 != null) on_SP2 = LambdaToSQLFactory.Get<T, T2, T3, T4>(SQLSort.SQLWhere, _on2, sqlsign);
        LambdaToSQLPlus on_SP3 = null;
        if (_on3 != null) on_SP3 = LambdaToSQLFactory.Get<T, T2, T3, T4>(SQLSort.SQLWhere, _on3, sqlsign);

        LambdaToSQLPlus columns_SP = null;
        if (_columns != null) columns_SP = LambdaToSQLFactory.Get<T, T2, T3, T4>(SQLSort.SQLFields, _columns, sqlsign);
        List<LambdaToSQLPlus[]> where_SP = new List<LambdaToSQLPlus[]>();
        int sign = 7000;
        foreach (var w in _where)
        {
            List<LambdaToSQLPlus> where_SP2 = new List<LambdaToSQLPlus>();
            foreach (var w2 in w)
            {
                where_SP2.Add(LambdaToSQLFactory.Get<T, T2, T3, T4>(SQLSort.SQLWhere, w2, sqlsign, sign)); sign += 100;
            }
            where_SP.Add(where_SP2.ToArray());
        }
        LambdaToSQLPlus groupby_SP = null;
        if (_groupby != null) groupby_SP = LambdaToSQLFactory.Get<T, T2, T3, T4>(SQLSort.SQLFields, _groupby, sqlsign);
        LambdaToSQLPlus having_SP = null;
        if (_having != null) having_SP = LambdaToSQLFactory.Get<T, T2, T3, T4>(SQLSort.SQLWhere, _having, sqlsign, 8000);
        LambdaToSQLPlus orderby_SP = null;
        if (_orderby != null) orderby_SP = LambdaToSQLFactory.Get<T, T2, T3, T4>(SQLSort.SQLOrder, _orderby, sqlsign);

        await base.GetList(columns_SP, new LambdaToSQLPlus[] { on_SP, on_SP2, on_SP3 }, where_SP, groupby_SP, having_SP, orderby_SP);
    }

}

public class DAL<T, T2, T3, T4, T5, TReturn> : DALBase<TReturn>
    where T : ModelBase<T>, new()
    where T2 : ModelBase<T2>, new()
    where T3 : ModelBase<T3>, new()
    where T4 : ModelBase<T4>, new()
    where T5 : ModelBase<T5>, new()
    where TReturn : class, new()
{
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, bool>> _columns;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, bool>> _on;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, bool>> _on2;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, bool>> _on3;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, bool>> _on4;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, bool>> _on5;
    private List<System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, bool>>[]> _where;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, bool>> _groupby;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, bool>> _having;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, bool>> _orderby;

    public DAL(JoinStyle[] join)
        : base(join)
    { }

    public DAL<T, T2, T3, T4, T5, TReturn> SetPageSize(int pagesize)
    {
        _pagesize = pagesize; return this;
    }
    public DAL<T, T2, T3, T4, T5, TReturn> SetPageIndex(int pageindex)
    {
        _pageindex = pageindex; return this;
    }
    public DAL<T, T2, T3, T4, T5, TReturn> Columns(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, bool>> columns)
    {
        _columns = columns; return this;
    }
    public DAL<T, T2, T3, T4, T5, TReturn> On(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, bool>> on, System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, bool>> on2, System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, bool>> on3, System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, bool>> on4, System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, bool>> on5)
    {
        _on = on; _on2 = on2; _on3 = on3; _on4 = on4; _on5 = on5; return this;
    }
    public DAL<T, T2, T3, T4, T5, TReturn> Where(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, bool>> where)
    {
        return Where(new Expression<Func<T, T2, T3, T4, T5, bool>>[] { where });
    }
    public DAL<T, T2, T3, T4, T5, TReturn> Where(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, bool>>[] wheres_or)
    {
        if (_where == null)
            _where = new List<Expression<Func<T, T2, T3, T4, T5, bool>>[]>();

        _where.Add(wheres_or);

        foreach (var w in wheres_or)
            base.AddTName(w.Parameters);

        return this;
    }

    public DAL<T, T2, T3, T4, T5, TReturn> GroupBy(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, bool>> groupby)
    {
        _groupby = groupby; return this;
    }
    public DAL<T, T2, T3, T4, T5, TReturn> Having(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, bool>> having)
    {
        _having = having; return this;
    }
    public DAL<T, T2, T3, T4, T5, TReturn> OrderBy(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, bool>> orderby)
    {
        _orderby = orderby; return this;
    }

    public async Task GetList()
    {
        AddTableName(typeof(T).Name.ToString2());
        AddTableName(typeof(T2).Name.ToString2());
        AddTableName(typeof(T3).Name.ToString2());
        AddTableName(typeof(T4).Name.ToString2());
        AddTableName(typeof(T5).Name.ToString2());

        LambdaToSQLPlus on_SP = null;
        if (_on != null) on_SP = LambdaToSQLFactory.Get<T, T2, T3, T4, T5>(SQLSort.SQLWhere, _on, sqlsign);
        LambdaToSQLPlus on_SP2 = null;
        if (_on2 != null) on_SP2 = LambdaToSQLFactory.Get<T, T2, T3, T4, T5>(SQLSort.SQLWhere, _on2, sqlsign);
        LambdaToSQLPlus on_SP3 = null;
        if (_on3 != null) on_SP3 = LambdaToSQLFactory.Get<T, T2, T3, T4, T5>(SQLSort.SQLWhere, _on3, sqlsign);
        LambdaToSQLPlus on_SP4 = null;
        if (_on4 != null) on_SP4 = LambdaToSQLFactory.Get<T, T2, T3, T4, T5>(SQLSort.SQLWhere, _on4, sqlsign);
        LambdaToSQLPlus on_SP5 = null;
        if (_on5 != null) on_SP5 = LambdaToSQLFactory.Get<T, T2, T3, T4, T5>(SQLSort.SQLWhere, _on5, sqlsign);

        LambdaToSQLPlus columns_SP = null;
        if (_columns != null) columns_SP = LambdaToSQLFactory.Get<T, T2, T3, T4, T5>(SQLSort.SQLFields, _columns, sqlsign);
        List<LambdaToSQLPlus[]> where_SP = new List<LambdaToSQLPlus[]>();
        int sign = 7000;
        foreach (var w in _where)
        {
            List<LambdaToSQLPlus> where_SP2 = new List<LambdaToSQLPlus>();
            foreach (var w2 in w)
            {
                where_SP2.Add(LambdaToSQLFactory.Get<T, T2, T3, T4, T5>(SQLSort.SQLWhere, w2, sqlsign, sign)); sign += 100;
            }
            where_SP.Add(where_SP2.ToArray());
        }
        LambdaToSQLPlus groupby_SP = null;
        if (_groupby != null) groupby_SP = LambdaToSQLFactory.Get<T, T2, T3, T4, T5>(SQLSort.SQLFields, _groupby, sqlsign);
        LambdaToSQLPlus having_SP = null;
        if (_having != null) having_SP = LambdaToSQLFactory.Get<T, T2, T3, T4, T5>(SQLSort.SQLWhere, _having, sqlsign, 8000);
        LambdaToSQLPlus orderby_SP = null;
        if (_orderby != null) orderby_SP = LambdaToSQLFactory.Get<T, T2, T3, T4, T5>(SQLSort.SQLOrder, _orderby, sqlsign);

        await base.GetList(columns_SP, new LambdaToSQLPlus[] { on_SP, on_SP2, on_SP3, on_SP4, on_SP5 }, where_SP, groupby_SP, having_SP, orderby_SP);
    }

}


public class DAL<T, T2, T3, T4, T5, T6, TReturn> : DALBase<TReturn>
    where T : ModelBase<T>, new()
    where T2 : ModelBase<T2>, new()
    where T3 : ModelBase<T3>, new()
    where T4 : ModelBase<T4>, new()
    where T5 : ModelBase<T5>, new()
    where T6 : ModelBase<T6>, new()
    where TReturn : class, new()
{
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, bool>> _columns;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, bool>> _on;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, bool>> _on2;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, bool>> _on3;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, bool>> _on4;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, bool>> _on5;
    private List<System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, bool>>[]> _where;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, bool>> _groupby;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, bool>> _having;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, bool>> _orderby;

    public DAL(JoinStyle[] join)
        : base(join)
    { }

    public DAL<T, T2, T3, T4, T5, T6, TReturn> SetPageSize(int pagesize)
    {
        _pagesize = pagesize; return this;
    }
    public DAL<T, T2, T3, T4, T5, T6, TReturn> SetPageIndex(int pageindex)
    {
        _pageindex = pageindex; return this;
    }
    public DAL<T, T2, T3, T4, T5, T6, TReturn> Columns(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, bool>> columns)
    {
        _columns = columns; return this;
    }
    public DAL<T, T2, T3, T4, T5, T6, TReturn> On(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, bool>> on, System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, bool>> on2, System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, bool>> on3, System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, bool>> on4, System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, bool>> on5)
    {
        _on = on; _on2 = on2; _on3 = on3; _on4 = on4; _on5 = on5; return this;
    }
    public DAL<T, T2, T3, T4, T5, T6, TReturn> Where(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, bool>> where)
    {
        return Where(new Expression<Func<T, T2, T3, T4, T5, T6, bool>>[] { where });
    }
    public DAL<T, T2, T3, T4, T5, T6, TReturn> Where(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, bool>>[] wheres_or)
    {
        if (_where == null)
            _where = new List<Expression<Func<T, T2, T3, T4, T5, T6, bool>>[]>();

        _where.Add(wheres_or);

        foreach (var w in wheres_or)
            base.AddTName(w.Parameters);

        return this;
    }

    public DAL<T, T2, T3, T4, T5, T6, TReturn> GroupBy(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, bool>> groupby)
    {
        _groupby = groupby; return this;
    }
    public DAL<T, T2, T3, T4, T5, T6, TReturn> Having(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, bool>> having)
    {
        _having = having; return this;
    }
    public DAL<T, T2, T3, T4, T5, T6, TReturn> OrderBy(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, bool>> orderby)
    {
        _orderby = orderby; return this;
    }

    public async Task GetList()
    {
        AddTableName(typeof(T).Name.ToString2());
        AddTableName(typeof(T2).Name.ToString2());
        AddTableName(typeof(T3).Name.ToString2());
        AddTableName(typeof(T4).Name.ToString2());
        AddTableName(typeof(T5).Name.ToString2());
        AddTableName(typeof(T6).Name.ToString2());

        LambdaToSQLPlus on_SP = null;
        if (_on != null) on_SP = LambdaToSQLFactory.Get<T, T2, T3, T4, T5, T6>(SQLSort.SQLWhere, _on, sqlsign);
        LambdaToSQLPlus on_SP2 = null;
        if (_on2 != null) on_SP2 = LambdaToSQLFactory.Get<T, T2, T3, T4, T5, T6>(SQLSort.SQLWhere, _on2, sqlsign);
        LambdaToSQLPlus on_SP3 = null;
        if (_on3 != null) on_SP3 = LambdaToSQLFactory.Get<T, T2, T3, T4, T5, T6>(SQLSort.SQLWhere, _on3, sqlsign);
        LambdaToSQLPlus on_SP4 = null;
        if (_on4 != null) on_SP4 = LambdaToSQLFactory.Get<T, T2, T3, T4, T5, T6>(SQLSort.SQLWhere, _on4, sqlsign);
        LambdaToSQLPlus on_SP5 = null;
        if (_on5 != null) on_SP5 = LambdaToSQLFactory.Get<T, T2, T3, T4, T5, T6>(SQLSort.SQLWhere, _on5, sqlsign);

        LambdaToSQLPlus columns_SP = null;
        if (_columns != null) columns_SP = LambdaToSQLFactory.Get<T, T2, T3, T4, T5, T6>(SQLSort.SQLFields, _columns, sqlsign);
        List<LambdaToSQLPlus[]> where_SP = new List<LambdaToSQLPlus[]>();
        int sign = 7000;
        foreach (var w in _where)
        {
            List<LambdaToSQLPlus> where_SP2 = new List<LambdaToSQLPlus>();
            foreach (var w2 in w)
            {
                where_SP2.Add(LambdaToSQLFactory.Get<T, T2, T3, T4, T5, T6>(SQLSort.SQLWhere, w2, sqlsign, sign)); sign += 100;
            }
            where_SP.Add(where_SP2.ToArray());
        }
        LambdaToSQLPlus groupby_SP = null;
        if (_groupby != null) groupby_SP = LambdaToSQLFactory.Get<T, T2, T3, T4, T5, T6>(SQLSort.SQLFields, _groupby, sqlsign);
        LambdaToSQLPlus having_SP = null;
        if (_having != null) having_SP = LambdaToSQLFactory.Get<T, T2, T3, T4, T5, T6>(SQLSort.SQLWhere, _having, sqlsign, 8000);
        LambdaToSQLPlus orderby_SP = null;
        if (_orderby != null) orderby_SP = LambdaToSQLFactory.Get<T, T2, T3, T4, T5, T6>(SQLSort.SQLOrder, _orderby, sqlsign);

        await base.GetList(columns_SP, new LambdaToSQLPlus[] { on_SP, on_SP2, on_SP3, on_SP4, on_SP5 }, where_SP, groupby_SP, having_SP, orderby_SP);
    }

}



public class DAL<T, T2, T3, T4, T5, T6, T7, TReturn> : DALBase<TReturn>
    where T : ModelBase<T>, new()
    where T2 : ModelBase<T2>, new()
    where T3 : ModelBase<T3>, new()
    where T4 : ModelBase<T4>, new()
    where T5 : ModelBase<T5>, new()
    where T6 : ModelBase<T6>, new()
    where T7 : ModelBase<T7>, new()
    where TReturn : class, new()
{
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>> _columns;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>> _on;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>> _on2;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>> _on3;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>> _on4;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>> _on5;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>> _on6;
    private List<System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>>[]> _where;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>> _groupby;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>> _having;
    private System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>> _orderby;

    public DAL(JoinStyle[] join)
        : base(join)
    { }

    public DAL<T, T2, T3, T4, T5, T6, T7, TReturn> SetPageSize(int pagesize)
    {
        _pagesize = pagesize; return this;
    }
    public DAL<T, T2, T3, T4, T5, T6, T7, TReturn> SetPageIndex(int pageindex)
    {
        _pageindex = pageindex; return this;
    }
    public DAL<T, T2, T3, T4, T5, T6, T7, TReturn> Columns(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>> columns)
    {
        _columns = columns; return this;
    }
    public DAL<T, T2, T3, T4, T5, T6, T7, TReturn> On(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>> on, System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>> on2, System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>> on3, System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>> on4, System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>> on5, System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>> on6)
    {
        _on = on; _on2 = on2; _on3 = on3; _on4 = on4; _on5 = on5; _on6 = on6; return this;
    }
    public DAL<T, T2, T3, T4, T5, T6, T7, TReturn> Where(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>> where)
    {
        return Where(new Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>>[] { where });
    }
    public DAL<T, T2, T3, T4, T5, T6, T7, TReturn> Where(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>>[] wheres_or)
    {
        if (_where == null)
            _where = new List<Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>>[]>();

        _where.Add(wheres_or);

        foreach (var w in wheres_or)
            base.AddTName(w.Parameters);

        return this;
    }
    public DAL<T, T2, T3, T4, T5, T6, T7, TReturn> GroupBy(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>> groupby)
    {
        _groupby = groupby; return this;
    }
    public DAL<T, T2, T3, T4, T5, T6, T7, TReturn> Having(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>> having)
    {
        _having = having; return this;
    }
    public DAL<T, T2, T3, T4, T5, T6, T7, TReturn> OrderBy(System.Linq.Expressions.Expression<Func<T, T2, T3, T4, T5, T6, T7, bool>> orderby)
    {
        _orderby = orderby; return this;
    }

    public async Task GetList()
    {
        AddTableName(typeof(T).Name.ToString2());
        AddTableName(typeof(T2).Name.ToString2());
        AddTableName(typeof(T3).Name.ToString2());
        AddTableName(typeof(T4).Name.ToString2());
        AddTableName(typeof(T5).Name.ToString2());
        AddTableName(typeof(T6).Name.ToString2());
        AddTableName(typeof(T7).Name.ToString2());

        LambdaToSQLPlus on_SP = null;
        if (_on != null) on_SP = LambdaToSQLFactory.Get<T, T2, T3, T4, T5, T6, T7>(SQLSort.SQLWhere, _on, sqlsign);
        LambdaToSQLPlus on_SP2 = null;
        if (_on2 != null) on_SP2 = LambdaToSQLFactory.Get<T, T2, T3, T4, T5, T6, T7>(SQLSort.SQLWhere, _on2, sqlsign);
        LambdaToSQLPlus on_SP3 = null;
        if (_on3 != null) on_SP3 = LambdaToSQLFactory.Get<T, T2, T3, T4, T5, T6, T7>(SQLSort.SQLWhere, _on3, sqlsign);
        LambdaToSQLPlus on_SP4 = null;
        if (_on4 != null) on_SP4 = LambdaToSQLFactory.Get<T, T2, T3, T4, T5, T6, T7>(SQLSort.SQLWhere, _on4, sqlsign);
        LambdaToSQLPlus on_SP5 = null;
        if (_on5 != null) on_SP5 = LambdaToSQLFactory.Get<T, T2, T3, T4, T5, T6, T7>(SQLSort.SQLWhere, _on5, sqlsign);
        LambdaToSQLPlus on_SP6 = null;
        if (_on6 != null) on_SP6 = LambdaToSQLFactory.Get<T, T2, T3, T4, T5, T6, T7>(SQLSort.SQLWhere, _on6, sqlsign);

        LambdaToSQLPlus columns_SP = null;
        if (_columns != null) columns_SP = LambdaToSQLFactory.Get<T, T2, T3, T4, T5, T6, T7>(SQLSort.SQLFields, _columns, sqlsign);

        List<object[]> where_SP = new List<object[]>();
        int sign = 7000;
        foreach (var w in _where)
        {
            List<object> where_SP2 = new List<object>();
            foreach (var w2 in w)
            {

                var _wSQL = LambdaToSQLFactory.Get<T, T2, T3, T4, T5, T6, T7>(SQLSort.SQLWhere, w2, sqlsign, sign);

                using (var db = conn.GetSqlSugarClient())
                {
                    var sqlParams = db.Queryable<T, T2, T3, T4, T5, T6, T7>(join).Where(w2).ToSql();
                    var sql = sqlParams.Key.Split("WHERE")[1];
                    var param = sqlParams.Value;
                }

                where_SP2.Add(_wSQL);
                where_SP2.Add(_wSQL);

                sign += 100;
            }
            where_SP.Add(where_SP2.ToArray());
        }
        LambdaToSQLPlus groupby_SP = null;
        if (_groupby != null) groupby_SP = LambdaToSQLFactory.Get<T, T2, T3, T4, T5, T6, T7>(SQLSort.SQLFields, _groupby, sqlsign);
        LambdaToSQLPlus having_SP = null;
        if (_having != null) having_SP = LambdaToSQLFactory.Get<T, T2, T3, T4, T5, T6, T7>(SQLSort.SQLWhere, _having, sqlsign, 8000);
        LambdaToSQLPlus orderby_SP = null;
        if (_orderby != null) orderby_SP = LambdaToSQLFactory.Get<T, T2, T3, T4, T5, T6, T7>(SQLSort.SQLOrder, _orderby, sqlsign);

        await base.GetList(columns_SP, new LambdaToSQLPlus[] { on_SP, on_SP2, on_SP3, on_SP4, on_SP5, on_SP6 }, where_SP, groupby_SP, having_SP, orderby_SP);
    }

}