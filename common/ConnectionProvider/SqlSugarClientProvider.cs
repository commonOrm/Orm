using Microsoft.Extensions.Configuration;
using Npgsql;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace common.ConnectionProvider
{
    public class SqlSugarClientProvider : IConnectionProvider
    {
        private readonly string _mssqlConnectionString;
        private readonly string _npgsqlConnectionString;

        public SqlSugarClientProvider(IConfiguration configuration)
        {
            _mssqlConnectionString = configuration["MssqlConnectionString"];
            _npgsqlConnectionString = configuration["NpgsqlConnectionString"];
        }

        public IDbConnection GetDbConnection()
        {
            return null;
        }

        public SqlSugarClient GetSqlSugarClient()
        {
            var expMethods = LambdaToSQLFactory.loadExpMethods();
            var isNpgsql = !string.IsNullOrWhiteSpace(_npgsqlConnectionString);
            
            var Db = new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString = isNpgsql ? _npgsqlConnectionString : _mssqlConnectionString,
                DbType = isNpgsql ? SqlSugar.DbType.PostgreSQL : SqlSugar.DbType.SqlServer,
                InitKeyType = InitKeyType.Attribute,
                IsAutoCloseConnection = true,
                MoreSettings = new ConnMoreSettings()
                {
                    //数据库存在大写字段的 ，需要把这个设为false ，并且实体和字段名称要一样
                    PgSqlIsAutoToLower = false
                },
                ConfigureExternalServices = new ConfigureExternalServices()
                {
                    SqlFuncServices = expMethods//set ext method
                }
            });

            return Db;
        }
    }
}
