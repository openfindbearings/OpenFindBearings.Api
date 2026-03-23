using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Specifications
{
    /// <summary>
    /// 用户搜索参数
    /// </summary>
    public class SearchUserParams
    {
        /// <summary>
        /// 关键词（搜索昵称、AuthUserId）
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 用户类型
        /// </summary>
        public UserType? UserType { get; set; }

        /// <summary>
        /// 是否活跃
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// 是否已合并（游客）
        /// </summary>
        public bool? IsMerged { get; set; }

        /// <summary>
        /// 页码
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每页条数
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// 排序字段
        /// </summary>
        public string? SortBy { get; set; } = "CreatedAt";

        /// <summary>
        /// 排序方向
        /// </summary>
        public string? SortOrder { get; set; } = "desc";
    }
}
