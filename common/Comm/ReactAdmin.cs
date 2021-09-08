using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Comm.ReactAdmin
{
    public enum SortBy { ASC, DESC }

    //_end=3&_order=DESC&_sort=ordernum&_start=0

    public abstract class ReactAdmin_RangeAndSort
    {
        /// <summary>
        /// 当前页索引
        /// </summary>
        public int _page { get; set; } = 0;
        /// <summary>
        /// 每页数量
        /// </summary>
        public int _pagesize { get; set; } = 10;
        /// <summary>
        /// 排序字段
        /// </summary>
        public string _sort { get; set; } = "id";
        /// <summary>
        /// 排序方式 默认ASC
        /// </summary>
        public SortBy _order { get; set; } = SortBy.ASC;
    }
}
