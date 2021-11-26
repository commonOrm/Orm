using commonXunit.init;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Dapper;
using System.Text;
using commonXunit.models;
using System.Linq;

namespace commonXunit
{
    public abstract class UnitTestAbc
    {
        protected IConnectionProvider conn;
        public UnitTestAbc()
        {
        }

        public async Task<int> _initReturnId()
        {
            await models.Product.DeleteWhere(t => t.id != 0);
            var pro = new models.Product() { Title = "测试1", Price = 123, Desc = "描述1" };
            var id = (await pro.Add()).ToInt32();
            return id;
        }
        public async Task<bool> _init()
        {
            //using (var db = conn.GetSqlSugarClient())
            //{
            //    int[] allIds = new int[] { 2, 3, 31 };
            //    db.Queryable<Product>().Where(it => allIds.Contains(it.id)).ToList();
            //}

            await models.Product.DeleteWhere(t => t.id != 0);
            var pro = new models.Product() { Title = "测试1", Price = 123, Desc = "描述1" };

            var id = (await pro.Add()).ToInt32();
            return id > 0;
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
            Assert.True(await models.Product.UpdateWhere(t => t.Title == newTitle, t => t.Title.lb_IsNotNullAndEqual("测试1")));
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
            Assert.Equal(123.0, dt.Rows[0]["Price"].ToDouble());
        }

        [Fact]
        public async Task Pager()
        {
            await _init();

            var pager = models.Product.Pager(t => t.id != 0, 0, 20);
            var dataList = await pager.GetDataList();

            Assert.Equal("测试1", dataList[0].Title.ToString());
        }

        [Fact]
        public async Task lambda_In()
        {
            var id = await _initReturnId();
            var ids = new List<int>() { id, 3, 4, 5 };
            var list = await models.Product.GetModelList(t => t.id.lb_In(ids.ToArray())).GetList();

            Assert.Equal("测试1", list[0].Title.ToString());
        }
        [Fact]
        public async Task lambda_NotIn()
        {
            var id = await _initReturnId();
            var ids = new List<int>() { id };
            var list = await models.Product.GetModelList(t => t.id.lb_NotIn(ids.ToArray())).GetList();

            Assert.True(list.Count == 0);
        }
        [Fact]
        public async Task lambda_Like()
        {
            await _init();

            var title = "测";
            var list = await models.Product.GetModelList(t => t.Title.lb_Like(title)).GetList();

            Assert.Equal("测试1", list[0].Title.ToString());
        }
        [Fact]
        public async Task lambda_IsNotNullAndEqual()
        {
            await _init();

            string title = null;
            var list = await models.Product.GetModelList(t => t.Title.lb_IsNotNullAndEqual(title) || t.Title.lb_IsNotNullAndEqual("测试1")).GetList();

            Assert.Equal("测试1", list[0].Title.ToString());
        }
    }

    public class UnitTest_SqlSugarClient_postgresql : UnitTestAbc
    {
        public UnitTest_SqlSugarClient_postgresql()
        {
            var db = new sqlsugarclient_postgresql();
            db.init();
            this.conn = db.conn;
        }
    }

    public class UnitTest_Postgresql : UnitTestAbc
    {
        public UnitTest_Postgresql()
        {
            var db = new postgresql();
            db.init();
            this.conn = db.conn;
        }
    }

    public class UnitTest_Mssql : UnitTestAbc
    {
        public UnitTest_Mssql()
        {
            var db = new mssql();
            db.init();
            this.conn = db.conn;
        }
    }
}
