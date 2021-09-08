using System;
using System.Collections.Generic;
using System.Text;

namespace common.ORM
{
    public abstract class SQLSign
    {
        public abstract string Create_ColumnEx(string column);
        public abstract string Create_InsertIntoSQLEx(string tablename,string columns,string column_args,string primarykeyname);
    }

    public class SQLSign_pgsql : SQLSign
    {
        public override string Create_ColumnEx(string column)
        {
            return "\""+ column + "\"";
        }

        public override string Create_InsertIntoSQLEx(string tablename, string columns, string column_args, string primarykeyname)
        {
            return  $@"INSERT INTO ""{tablename}""({columns}) VALUES ({column_args}) RETURNING ""{primarykeyname}"";";
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
            return $@"INSERT INTO ""{tablename}""({columns}) VALUES ({column_args});select @@IDENTITY";
        }
    }
}
