namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 同步结果DTO
    /// </summary>
    public class SyncResultDto
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        public Guid TaskId { get; set; }

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
        /// 处理时间（秒）
        /// </summary>
        public double ElapsedSeconds { get; set; }

        /// <summary>
        /// 错误详情列表
        /// </summary>
        public List<SyncErrorDto> Errors { get; set; } = new();
    }

}
