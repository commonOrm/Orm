using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Dapper;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace commonXunit.init
{
    public abstract class dbsql
    {
        protected IConnectionProvider conn;
        protected List<Type> tables;

        public abstract void init();
        protected void GetAllTable()
        {
            tables = new List<Type>();
            var currentDomain = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(t => t.FullName.Contains("commonXunit"));
            var currentDomainTypes = currentDomain.DefinedTypes;
            foreach (var ty in currentDomainTypes)
            {
                if (ty.BaseType.ToString().IndexOf("ModelBase") == 0)
                {
                    tables.Add(ty);
                    //System.Diagnostics.Debug.WriteLine(ty.Name);
                    //var tableName = ty.Name;
                }
            }
        }
        protected abstract Task DropAllTable();
        protected abstract Task CreateAllTableAndInit();
    }

    public class postgresql : dbsql
    {
        public override void init()
        {
            var services = new ServiceCollection();
            services.AddHttpClient();
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var builder = new ConfigurationBuilder()
               //.SetBasePath()
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddJsonFile("appsettings.Development.Postgresql.json", optional: true, reloadOnChange: true)
               .AddEnvironmentVariables();
            var Configuration = builder.Build();
            services.AddSingleton<IConfiguration>(Configuration);

            services.AddSingleton<IConnectionProvider, common.ConnectionProvider.NpgsqlConnectionProvider>();
            ServiceLocator.Instance = services.BuildServiceProvider();
            conn = new common.ConnectionProvider.NpgsqlConnectionProvider(Configuration);
            ServiceLocator.conn = conn;

            DropAllTable().Wait();
            CreateAllTableAndInit().Wait();
        }

        protected async override Task DropAllTable()
        {
            GetAllTable();
            string[] TableNames = tables.Select(t => t.Name).ToArray();

            StringBuilder output = new StringBuilder();
            foreach (var tablename in TableNames)
            {
                output.AppendLine(string.Format("DROP TABLE IF EXISTS \"{0}\";", tablename.ToLower()));
                output.AppendLine(string.Format("DROP TABLE IF EXISTS \"{0}\";", tablename));
            }
            using (var connection = conn.GetDbConnection())
            {
                var result1 = await connection.ExecuteAsync(output.ToString());
            }
        }
        protected async override Task CreateAllTableAndInit()
        {
            GetAllTable();

            StringBuilder output = new StringBuilder();
            foreach (var ty in tables)
            {
                var tableName = ty.Name;
                output.AppendLine(string.Format("CREATE TABLE \"{0}\"", tableName));
                output.AppendLine(string.Format("("));
                int i = 0;
                var props = ty.GetProperties();
                foreach (var prop in props)
                {
                    i++;
                    var propName = prop.Name;
                    var propType = prop.PropertyType;
                    bool isKey = prop.GetCustomAttributes(typeof(KeyAttribute), false).Length > 0;

                    var databaseType = ConventType(propType);
                    if (isKey && propType != typeof(int))
                        throw new Exception("主键必须是 int 类型");

                    if (isKey)
                    {
                        output.AppendLine(string.Format("    \"{0}\" serial NOT NULL,", propName));
                        output.AppendLine(string.Format("    CONSTRAINT {0}_pkey PRIMARY KEY (\"{1}\"),", tableName, propName));
                    }
                    else
                    {
                        output.AppendLine(string.Format("    \"{0}\" {1}{2}", propName, databaseType, i == props.Length ? "" : ","));
                    }
                }
                output.AppendLine(string.Format(");"));
            }
            string SQL = output.ToString();

            using (var connection = conn.GetDbConnection())
            {
                var affectedRows = await connection.ExecuteAsync(SQL);
            }
        }
        private string ConventType(Type type)
        {
            var result = "";
            var typeName = type.Name.ToLower();

            if (typeName == "nullable`1" && type.GenericTypeArguments.Length > 0)
            { typeName = type.GenericTypeArguments[0].Name.ToLower(); }

            System.Diagnostics.Debug.WriteLine("typeName:" + typeName);
            switch (typeName)
            {
                case "int":
                case "int32":
                case "int64":
                    result = "integer";
                    break;
                case "double":
                case "float":
                case "decimal":
                    result = "double precision";
                    break;
                case "string":
                    result = "character varying";
                    break;
                case "bool":
                case "boolean":
                    result = "boolean";
                    break;
                case "datetime":
                    result = "timestamp without time zone";
                    break;
                case "guid":
                    result = "uuid";
                    break;
                default:
                    throw new MyException($"class DatabaseStructure function ConventType：{typeName} do not recognized");
            }
            return result;
        }
    }

    public class mssql : dbsql
    {
        public override void init()
        {
            var services = new ServiceCollection();
            services.AddHttpClient();
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var builder = new ConfigurationBuilder()
               //.SetBasePath()
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .AddJsonFile("appsettings.Development.Mssql.json", optional: false, reloadOnChange: true)
               .AddEnvironmentVariables();
            var Configuration = builder.Build();
            services.AddSingleton<IConfiguration>(Configuration);

            services.AddSingleton<IConnectionProvider, common.ConnectionProvider.MssqlConnectionProvider>();
            ServiceLocator.Instance = services.BuildServiceProvider();
            conn = new common.ConnectionProvider.MssqlConnectionProvider(Configuration);
            ServiceLocator.conn = conn;

            DropAllTable().Wait();
            CreateAllTableAndInit().Wait();
        }

        protected async override Task DropAllTable()
        {
            GetAllTable();
            string[] TableNames = tables.Select(t => t.Name).ToArray();

            StringBuilder output = new StringBuilder();
            foreach (var tablename in TableNames)
                output.AppendLine(string.Format("DROP TABLE IF EXISTS [{0}];", tablename));
            using (var connection = conn.GetDbConnection())
            {
                var result1 = await connection.ExecuteAsync(output.ToString());
            }
        }
        protected async override Task CreateAllTableAndInit()
        {
            GetAllTable();
            StringBuilder output = new StringBuilder();
            foreach (var ty in tables)
            {
                var tableName = ty.Name;
                output.AppendLine(string.Format("CREATE TABLE [{0}]", tableName));
                output.AppendLine(string.Format("("));
                int i = 0;
                var props = ty.GetProperties();
                foreach (var prop in props)
                {
                    i++;
                    var propName = prop.Name;
                    var propType = prop.PropertyType;
                    bool isKey = prop.GetCustomAttributes(typeof(KeyAttribute), false).Length > 0;

                    var databaseType = ConventType(propType);
                    if (isKey && propType != typeof(int))
                        throw new Exception("主键必须是 int 类型");

                    if (isKey)
                    {
                        output.AppendLine(string.Format("    [{0}] int PRIMARY KEY IDENTITY(1,1),", propName));
                    }
                    else
                    {
                        output.AppendLine(string.Format("    [{0}] {1}{2}", propName, databaseType, i == props.Length ? "" : ","));
                    }
                }
                output.AppendLine(string.Format(");"));
            }
            string SQL = output.ToString();

            using (var connection = conn.GetDbConnection())
            {
                var affectedRows = await connection.ExecuteAsync(SQL);
            }
        }
        private string ConventType(Type type)
        {
            var result = "";
            var typeName = type.Name.ToLower();

            if (typeName == "nullable`1" && type.GenericTypeArguments.Length > 0)
            { typeName = type.GenericTypeArguments[0].Name.ToLower(); }

            System.Diagnostics.Debug.WriteLine("typeName:" + typeName);
            switch (typeName)
            {
                case "int":
                case "int32":
                case "int64":
                    result = "int";
                    break;
                case "double":
                case "float":
                case "decimal":
                    result = "float";
                    break;
                case "string":
                    result = "nvarchar(max)";
                    break;
                case "bool":
                case "boolean":
                    result = "bit";
                    break;
                case "datetime":
                    result = "datetime";
                    break;
                case "guid":
                    result = "guid";
                    break;
                default:
                    throw new MyException($"class DatabaseStructure function ConventType：{typeName} do not recognized");
            }
            return result;
        }
    }
}
