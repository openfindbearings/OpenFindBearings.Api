using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFindBearings.Domain.Common
{
    /// <summary>
    /// 领域层分页结果
    /// 注意：这是 Domain 层的版本，与 Application 层的分开
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}
