namespace OpenFindBearings.Application.Common.Enums
{
    /// <summary>
    /// 同步模式
    /// </summary>
    public enum SyncMode
    {
        /// <summary>
        /// 仅创建（如果存在则报错）
        /// </summary>
        Create = 0,

        /// <summary>
        /// 仅更新（如果不存在则报错）
        /// </summary>
        Update = 1,

        /// <summary>
        /// 存在则更新，不存在则创建
        /// </summary>
        Upsert = 2
    }
}
