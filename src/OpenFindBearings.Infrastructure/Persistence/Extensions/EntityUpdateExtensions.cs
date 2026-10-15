using Microsoft.EntityFrameworkCore;

namespace OpenFindBearings.Infrastructure.Persistence.Extensions
{
    /// <summary>
    /// EF 更新助手（v2.13.2 防御性）：安全地把实体标记为 Modified——
    /// 若实体已被跟踪为 Added（同一请求内新建后又走"更新"路径），保持 Added 不动，
    /// 避免 EF 把 INSERT 降级为 UPDATE 不存在行 → DbUpdateConcurrencyException
    /// （成就写路径 UpdateUnlock 踩过的坑，此处为其余仓储补齐同类防线）。
    /// </summary>
    public static class EntityUpdateExtensions
    {
        /// <summary>
        /// 标记为已修改：Added 实体保持 Added（防降级），其余（Detached/Unchanged/Modified）置 Modified。
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="context">当前 DbContext</param>
        /// <param name="entity">待标记实体</param>
        public static void MarkModified<TEntity>(this DbContext context, TEntity entity) where TEntity : class
        {
            var entry = context.Entry(entity);
            if (entry.State != EntityState.Added)
                entry.State = EntityState.Modified;
        }
    }
}