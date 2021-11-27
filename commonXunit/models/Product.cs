using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace commonXunit.models
{
    public class Product : ModelBase<Product>
    {
        [Key]
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int id { get; set; }
        public string Title { get; set; }
        public double Price { get; set; }
        public string Desc { get; set; }
    }

    public class ProductDetail : ModelBase<ProductDetail>
    {
        [Key]
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int id { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public double Price { get; set; }
        public int Count { get; set; }
    }

    public class ProductDetail2 : ModelBase<ProductDetail2>
    {
        [Key]
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int id { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public double Price { get; set; }
        public int Count { get; set; }
    }
}
