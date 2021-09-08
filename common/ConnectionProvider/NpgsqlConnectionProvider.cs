using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace common.ConnectionProvider
{
    public class NpgsqlConnectionProvider : IConnectionProvider
    {
        private readonly string _connectionString;

        public NpgsqlConnectionProvider(IConfiguration configuration)
        {
            _connectionString = configuration["NpgsqlConnectionString"];
        }

        public IDbConnection GetDbConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }
}
