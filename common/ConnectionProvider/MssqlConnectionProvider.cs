using Microsoft.Extensions.Configuration;
using Npgsql;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace common.ConnectionProvider
{
    public class MssqlConnectionProvider : IConnectionProvider
    {
        private readonly string _connectionString;
        public bool MssqlEqualOrLessThan2008 = false;

        public MssqlConnectionProvider(IConfiguration configuration)
        {
            MssqlEqualOrLessThan2008 = configuration["MssqlEqualOrLessThan2008"].ToString2().ToLower() == "true".ToLower();
            _connectionString = configuration["MssqlConnectionString"];
        }

        public IDbConnection GetDbConnection()
        {
            return new SqlConnection(_connectionString);
        }
        public SqlSugarClient GetSqlSugarClient()
        {
            return null;
        }
    }
}
