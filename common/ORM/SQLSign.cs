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
                sqlsign = new SQLSign_mssql();
            else if (conn is NpgsqlConnectionProvider)
                sqlsign = new SQLSign_pgsql();

            return sqlsign;
        }

        public abstract string Create_ColumnEx(string column);
        public abstract string Create_InsertIntoSQLEx(string tablename, string columns, string column_args, string primarykeyname);
        public abstract string Create_GetListSQLEx(string fields, string tablename, string where, string orderby, int top);
        public abstract string Create_GetCountSQLEx(string tablename, string where);
        public abstract string Create_GetPagerSQLEx(string tablename, string where, string orderby, int PageSize, int PageIndex);
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
        public override string Create_GetCountSQLEx( string tablename, string where)
        {
            return @$"SELECT COUNT(*) FROM ""{tablename}"" WHERE {where}";
        }

        public override string Create_GetPagerSQLEx( string tablename, string where, string orderby, int PageSize,int PageIndex)
        {
            return @$"SELECT * FROM ""{tablename}"" 
                            WHERE {where} 
                            ORDER BY {orderby} 
                            LIMIT {PageSize} OFFSET {PageIndex * PageSize}";
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
    }
}
