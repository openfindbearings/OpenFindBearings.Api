namespace OpenFindBearings.Application.Common.Models
{
    /// <summary>
    /// 批量操作单个项的结果
    /// </summary>
    public class BatchItemResult
    {
        /// <summary>
        /// 项的唯一标识（如型号、名称等）
        /// </summary>
        public string Identifier { get; set; } = string.Empty;

        /// <summary>
        /// 执行的操作（created/updated/skipped/failed）
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// 生成的ID（如果是创建操作）
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// 错误信息（如果失败）
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess => Error == null;

        public BatchItemResult() { }

        public BatchItemResult(string identifier, string action, Guid? id = null, string? error = null)
        {
            Identifier = identifier;
            Action = action;
            Id = id;
            Error = error;
        }

        public static BatchItemResult Success(string identifier, string action, Guid? id = null)
            => new(identifier, action, id);

        public static BatchItemResult Failed(string identifier, string error)
            => new(identifier, "failed", null, error);
    }
}
