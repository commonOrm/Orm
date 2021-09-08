using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace commonXunit.models
{
    class Product : ModelBase<Product>
    {
        [Key]
        public int id { get; set; }
        public string Title { get; set; }
        public double Price { get; set; }
        public string Desc { get; set; }
    }
}
