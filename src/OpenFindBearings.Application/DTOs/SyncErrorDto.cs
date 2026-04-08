namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 同步错误DTO
    /// </summary>
    public class SyncErrorDto
    {
        /// <summary>
        /// 项目标识
        /// </summary>
        public string Identifier { get; set; } = string.Empty;

        /// <summary>
        /// 错误信息
        /// </summary>
        public string Error { get; set; } = string.Empty;
    }
}
