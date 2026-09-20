namespace OpenFindBearings.Application.Services
{
    /// <summary>
    /// 对象存储服务抽象（S3 标准协议语义）。
    /// 改动说明（v1.5.0）：取代从未被消费的 IFileService 死代码——用户上传（头像/Logo/证照材料）
    /// 落盘从"端点直写 wwwroot 本地盘"改为走本抽象，生产实现为 MinIO（S3 协议），
    /// 开发实现为本地盘，配置 FileStorage:Provider 切换，与 Sync 的对象存储模型同构。
    /// key 为对象相对路径（如 uploads/avatars/{id}_{ts}.jpg），URL 形态保持 "/{key}"
    /// 相对键入库，展示层（media 服务 + MediaBaseUrl 拼接）不变。
    /// </summary>
    public interface IObjectStorageService
    {
        /// <summary>
        /// 上传对象，返回可入库的相对 URL（/{key} 或 CdnDomain 拼接）
        /// </summary>
        /// <param name="key">对象键（相对路径，如 uploads/avatars/xxx.jpg）</param>
        /// <param name="content">文件内容</param>
        /// <param name="contentType">MIME 类型</param>
        Task<string> UploadAsync(string key, byte[] content, string contentType, CancellationToken ct = default);

        /// <summary>删除对象；不存在或出错返回 false（不抛）</summary>
        Task<bool> DeleteAsync(string key, CancellationToken ct = default);

        /// <summary>检查对象是否存在</summary>
        Task<bool> ExistsAsync(string key, CancellationToken ct = default);

        /// <summary>由对象键得展示 URL（不访问存储）</summary>
        string GetUrl(string key);
    }
}
