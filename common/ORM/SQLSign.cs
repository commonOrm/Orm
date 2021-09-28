using common.ConnectionProvider;
using System;
using System.Collections.Generic;
using System.Text;

namespace common.ORM
{
    public abstract class SQLSign
    {
        public static SQLSign Create(IConnectionProvider conn)
        {
            SQLSign sqlsign = null;

            if (conn is MssqlConnectionProvider)
            {
                if (((MssqlConnectionProvider)conn).MssqlEqualOrLessThan2008)
                    sqlsign = new SQLSign_mssql_equalOrLessThan2008();
                else
                    sqlsign = new SQLSign_mssql();
            }
            else if (conn is NpgsqlConnectionProvider)
                sqlsign = new SQLSign_pgsql();

            return sqlsign;
        }

        public abstract string Create_ColumnEx(string column);
        public abstract string Create_InsertIntoSQLEx(string tablename, string columns, string column_args, string primarykeyname);
        public abstract string Create_GetListSQLEx(string fields, string tablename, string where, string orderby, int top);
        public abstract string Create_GetCountSQLEx(string tablename, string where);
        public abstract string Create_GetPagerSQLEx(string tablename, string where, string orderby, int PageSize, int PageIndex);

        public abstract string Create_GetInSQLEx();
        public abstract string Create_GetNotInSQLEx();
    }

    public class SQLSign_pgsql : SQLSign
    {
        public override string Create_ColumnEx(string column)
        {
            return "\"" + column + "\"";
        }

        public override string Create_InsertIntoSQLEx(string tablename, string columns, string column_args, string primarykeyname)
        {
            return $@"INSERT INTO ""{tablename}""({columns}) VALUES ({column_args}) RETURNING ""{primarykeyname}"";";
        }

        public override string Create_GetListSQLEx(string fields, string tablename, string where, string orderby, int top)
        {
            return @$"SELECT {fields} FROM ""{tablename}"" WHERE {where} ORDER BY {orderby} LIMIT {top}";
        }
        public override string Create_GetCountSQLEx(string tablename, string where)
        {
            return @$"SELECT COUNT(*) FROM ""{tablename}"" WHERE {where}";
        }

        public override string Create_GetPagerSQLEx(string tablename, string where, string orderby, int PageSize, int PageIndex)
        {
            return @$"SELECT * FROM ""{tablename}"" 
                            WHERE {where} 
                            ORDER BY {orderby} 
                            LIMIT {PageSize} OFFSET {PageIndex * PageSize}";
        }
        public override string Create_GetInSQLEx()
        {
            return " \"{0}\" in (select unnest({1})) ";
        }
        public override string Create_GetNotInSQLEx()
        {
            return " \"{0}\" not in (select unnest({1})) ";
        }
    }

    public class SQLSign_mssql : SQLSign
    {
        public override string Create_ColumnEx(string column)
        {
            return "[" + column + "]";
        }

        public override string Create_InsertIntoSQLEx(string tablename, string columns, string column_args, string primarykeyname)
        {
            return $@"INSERT INTO [{tablename}]({columns}) VALUES ({column_args});select @@IDENTITY";
        }
        public override string Create_GetListSQLEx(string fields, string tablename, string where, string orderby, int top)
        {
            return @$"SELECT {fields} FROM [{tablename}] WHERE {where} ORDER BY {orderby} OFFSET 0 ROWS FETCH NEXT {top} ROWS ONLY";
        }
        public override string Create_GetCountSQLEx(string tablename, string where)
        {
            return @$"SELECT COUNT(*) FROM [{tablename}] WHERE {where}";
        }
        public override string Create_GetPagerSQLEx(string tablename, string where, string orderby, int PageSize, int PageIndex)
        {
            return @$"SELECT * FROM [{tablename}] 
                            WHERE {where} 
                            ORDER BY {orderby} 
                            OFFSET {PageIndex * PageSize} ROWS FETCH NEXT {PageSize} ROWS ONLY";
        }
        public override string Create_GetInSQLEx()
        {
            return " \"{0}\" in ({1}) ";
        }
        public override string Create_GetNotInSQLEx()
        {
            return " \"{0}\" not in ({1}) ";
        }
    }

    public class SQLSign_mssql_equalOrLessThan2008 : SQLSign_mssql
    {
        public override string Create_GetListSQLEx(string fields, string tablename, string where, string orderby, int top)
        { 
            StringBuilder strSql = new StringBuilder();
            /*SQL2005以上支持 ROW_NUMBER() OVER()  分页方式*/
            strSql.AppendFormat("SELECT TOP {0} {1} FROM ", top, fields);
            strSql.AppendFormat("(");
            strSql.AppendFormat("  SELECT ROW_NUMBER() OVER (ORDER BY {0}) AS RowNumber, {1} FROM {2} where {3}", orderby, fields, tablename, where);
            strSql.AppendFormat(") as A ");
            strSql.AppendFormat("WHERE RowNumber > 0");

            return strSql.ToString();
        }
        public override string Create_GetPagerSQLEx(string tablename, string where, string orderby, int PageSize, int PageIndex)
        { 
            StringBuilder strSql = new StringBuilder();
            /*SQL2005以上支持 ROW_NUMBER() OVER()  分页方式*/
            strSql.AppendFormat("SELECT TOP {0} {1} FROM ", PageSize, "*");
            strSql.AppendFormat("(");
            strSql.AppendFormat("  SELECT ROW_NUMBER() OVER (ORDER BY {0}) AS RowNumber, {1} FROM {2} where {3}", orderby, "*", tablename, where);
            strSql.AppendFormat(") as A ");
            strSql.AppendFormat("WHERE RowNumber > ( {0} * {1} )", PageSize, PageIndex);

            return strSql.ToString();
        }
    }
}
