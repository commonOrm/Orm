using commonXunit.init;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Dapper;
using System.Text;

namespace commonXunit
{
    public abstract class UnitTestAbc
    {
        protected IConnectionProvider conn;
        public UnitTestAbc()
        {

        }

        [Fact]
        public async Task Add()
        {
            var pro = new models.Product() { Title = "≤‚ ‘1", Price = 123, Desc = "√Ë ˆ1" };
            Assert.Equal((await pro.Add()).ToString(), 1.ToString());
        }
        //[Fact]
        //public async Task Update()
        //{
        //    var pro = new models.Product() { Title = "≤‚ ‘1", Price = 123, Desc = "√Ë ˆ1" };
        //    await pro.Add();
        //}
    }

    public class UnitTest_Postgresql : UnitTestAbc
    {
        public UnitTest_Postgresql()
        {
            new postgresql().init();
        }
    }

    public class UnitTest_Mssql : UnitTestAbc
    {
        public UnitTest_Mssql()
        {
            new mssql().init();
        }
    }
}
