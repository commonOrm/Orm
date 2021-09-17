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

        public async Task<bool> _init()
        {
            await models.Product.DeleteWhere(t => t.id != 0);
            var pro = new models.Product() { Title = "测试1", Price = 123, Desc = "描述1" };
            return (await pro.Add()).ToInt32() > 0;
        }

        [Fact]
        public async Task Add()
        {
            Assert.True((await _init()));
        }
        [Fact]
        public async Task Exists()
        {
            await _init();

            Assert.True(await models.Product.Exists(t => t.Title == "测试1"));
        }
        [Fact]
        public async Task Update()
        {
            await _init();

            var newTitle = "测试1,update";
            var model = await models.Product.GetModelWhere(t => t.id != 0);
            model.Title = newTitle;
            Assert.True(await model.Update());
            model = await models.Product.GetModelWhere(t => t.id != 0);
            Assert.Equal(newTitle, model.Title);
        }
        [Fact]
        public async Task UpdateWhere()
        {
            await _init();

            var newTitle = "测试1,update";
            Assert.True(await models.Product.UpdateWhere(t => t.Title == newTitle, t => t.id != 0));
            var model = await models.Product.GetModelWhere(t => t.id != 0);
            Assert.Equal(newTitle, model.Title);
        }
        [Fact]
        public async Task Delete()
        {
            await _init();

            var model = await models.Product.GetModelWhere(t => t.id != 0);
            Assert.True(await model.Delete());
        }
        [Fact]
        public async Task DeleteWhere()
        {
            await _init();

            Assert.True(await models.Product.DeleteWhere(t => t.Title == "测试1"));
        }
        [Fact]
        public async Task GetModel()
        {
            await _init();

            var model = await models.Product.GetModelWhere(t => t.Title == "测试1");
            var model2 = await models.Product.GetModel(model.id);
            Assert.Equal("测试1", model2.Title);
        }
        [Fact]
        public async Task GetModelWhere()
        {
            await _init();

            var model = await models.Product.GetModelWhere(t => t.Title == "测试1");
            Assert.Equal("测试1", model.Title);
        }

        [Fact]
        public async Task GetModelList()
        {
            await _init();

            var modelList = await models.Product.GetModelList(t => t.Title == "测试1").GetList();
            Assert.Equal("测试1", modelList[0].Title);
        }


        [Fact]
        public async Task GetModelCount()
        {
            await _init();

            var count = await models.Product.GetModelList(t => t.Title == "测试1").GetCount();
            Assert.True(count == 1);
        }

        [Fact]
        public async Task GetFieldList()
        {
            await _init();

            var dt = await models.Product.GetFieldList(t => t.Title.lb_ColumeName() && t.Price.lb_ColumeName(), t => t.id != 0);
            Assert.Equal("测试1", dt.Rows[0][0].ToString());
            Assert.Equal(123, dt.Rows[0]["Price"].ToDouble());
        }

        [Fact]
        public async Task Pager()
        {
            await _init();

            var pager = models.Product.Pager(t => t.id != 0, 0, 20);
            var dataList = await pager.GetDataList();

            Assert.Equal("测试1", dataList[0].Title.ToString());
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
