namespace OpenFindBearings.Application.Common.Models
{
    /// <summary>
    /// 批量操作结果
    /// </summary>
    public class BatchResult
    {
        /// <summary>
        /// 成功数量
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败数量
        /// </summary>
        public int FailCount { get; set; }

        /// <summary>
        /// 总数量
        /// </summary>
        public int TotalCount => SuccessCount + FailCount;

        /// <summary>
        /// 各项目结果列表
        /// </summary>
        public List<BatchItemResult> Items { get; set; } = new();

        /// <summary>
        /// 添加成功项
        /// </summary>
        public void AddSuccess(string identifier, string action, Guid? id = null)
        {
            Items.Add(BatchItemResult.Success(identifier, action, id));
            SuccessCount++;
        }

        /// <summary>
        /// 添加失败项
        /// </summary>
        public void AddFailed(string identifier, string error)
        {
            Items.Add(BatchItemResult.Failed(identifier, error));
            FailCount++;
        }

        /// <summary>
        /// 合并另一个结果
        /// </summary>
        public void Merge(BatchResult other)
        {
            SuccessCount += other.SuccessCount;
            FailCount += other.FailCount;
            Items.AddRange(other.Items);
        }
    }
}
