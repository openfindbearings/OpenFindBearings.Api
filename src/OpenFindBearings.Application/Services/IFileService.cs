namespace OpenFindBearings.Application.Services
{
    /// <summary>
    /// 文件上传结果
    /// </summary>
    public class UploadResult
    {
        public bool Success { get; set; }
        public string? FileName { get; set; }
        public string? OriginalFileName { get; set; }
        public string? FileUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public long FileSize { get; set; }
        public string? ContentType { get; set; }
    }

    /// <summary>
    /// 文件服务接口
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// 上传单个文件
        /// </summary>
        /// <param name="file">文件流</param>
        /// <param name="fileName">原始文件名</param>
        /// <param name="subDirectory">子目录</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task<UploadResult> UploadAsync(Stream file, string fileName, string? subDirectory = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 上传多个文件
        /// </summary>
        /// <param name="files">文件列表 (文件名, 流)</param>
        /// <param name="subDirectory">子目录</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task<List<UploadResult>> UploadManyAsync(List<(string FileName, Stream Stream)> files, string? subDirectory = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="fileUrl">文件URL或路径</param>
        Task<bool> DeleteAsync(string fileUrl);

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        /// <param name="fileUrl">文件URL或路径</param>
        Task<bool> ExistsAsync(string fileUrl);

        /// <summary>
        /// 获取文件访问URL
        /// </param>
        /// <param name="relativePath">相对路径</param>
        string GetFileUrl(string relativePath);
    }
}
