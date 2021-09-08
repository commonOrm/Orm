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
            var pro = new models.Product() { Title = "²âÊÔ1", Price = 123, Desc = "ÃèÊö1" };
            Assert.True((await pro.Add()).ToInt32() > 0);
        }

        [Fact]
        public async Task Delete()
        {
            await Add();
            Assert.True(await models.Product.DeleteWhere(t => t.id != 0));
        }

        [Fact]
        public async Task Select()
        {
            await Delete();
            await Add();
            var model = await models.Product.GetModelWhere(t => t.id != 0);
            Assert.Equal("²âÊÔ1", model.Title);
        }
        [Fact]
        public async Task Update()
        {
            var newTitle = "²âÊÔ1,update";

            await Delete();
            await Add();
            var model = await models.Product.GetModelWhere(t => t.id != 0);
            model.Title = newTitle;
            Assert.True(await model.Update());
            model = await models.Product.GetModelWhere(t => t.id != 0);
            Assert.Equal(newTitle, model.Title);
        }
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
