using Microsoft.Extensions.Configuration;
using Npgsql;
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

        public MssqlConnectionProvider(IConfiguration configuration)
        {
            _connectionString = configuration["MssqlConnectionProvider"];
        }

        public IDbConnection GetDbConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
