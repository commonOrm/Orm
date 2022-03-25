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
    public class UnitTest02
    {
        public UnitTest02() { }

        [Fact]
        public void Test_IsNull()
        {
            Product product = null;
            Assert.True(product.IsNull());

            object obj = null;
            Assert.True(obj.IsNull());

            ////dynamic不能调用IsNull(), 也不能为其设置扩展方法////
            //dynamic dyn = null;
            //Assert.True(dyn.IsNull());
        }
    }
}
