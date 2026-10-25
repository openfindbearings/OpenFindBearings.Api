using System.ComponentModel.DataAnnotations.Schema;
using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 型号线索实体
    /// 记录发布寻货与商家应答中出现、但平台型号库尚未收录的型号，供管理员判断是否建档
    /// 线索只做证据累积，不影响业务单据本身，也不自动写入型号库
    /// </summary>
    public class BearingLead : BaseEntity
    {
        /// <summary>
        /// 型号原文（展示用，保留用户填写的大小写）
        /// </summary>
        public string PartNumber { get; private set; } = string.Empty;

        /// <summary>
        /// 型号去重键（去首尾空格并转小写）
        /// 唯一索引列，同一型号不同写法的大小写差异合并为一条线索
        /// </summary>
        public string PartNumberKey { get; private set; } = string.Empty;

        /// <summary>
        /// 期望品牌（可空，仅发布寻货会带，供管理员建档时参考）
        /// </summary>
        public string? Brand { get; private set; }

        /// <summary>
        /// 最近一次出现来源
        /// </summary>
        public BearingLeadSource Source { get; private set; }

        /// <summary>
        /// 累计出现次数
        /// </summary>
        public int OccurrenceCount { get; private set; }

        /// <summary>
        /// 首次出现时间（UTC）
        /// </summary>
        public DateTime FirstSeenAt { get; private set; }

        /// <summary>
        /// 最近出现时间（UTC）
        /// </summary>
        public DateTime LastSeenAt { get; private set; }

        /// <summary>
        /// 是否已被管理员忽略（忽略后不再出现在待建档列表，但出现次数仍继续累加）
        /// </summary>
        public bool IsIgnored { get; private set; }

        /// <summary>
        /// 忽略时间（UTC）
        /// </summary>
        public DateTime? IgnoredAt { get; private set; }

        /// <summary>
        /// 执行忽略的管理员用户 ID
        /// </summary>
        public Guid? IgnoredByUserId { get; private set; }

        /// <summary>
        /// 是否已入库（计算态，不入库）
        /// 由仓储在查询时按"型号库是否已有同型号"回填，供上层判定展示状态
        /// </summary>
        [NotMapped]
        public bool IsResolved { get; private set; }

        /// <summary>
        /// 私有构造函数，仅供 EF Core 使用
        /// </summary>
        private BearingLead() { }

        /// <summary>
        /// 创建型号线索
        /// </summary>
        /// <param name="partNumber">型号原文（去首尾空格后写入）</param>
        /// <param name="brand">期望品牌，可空</param>
        /// <param name="source">出现来源</param>
        public BearingLead(string partNumber, string? brand, BearingLeadSource source)
        {
            if (string.IsNullOrWhiteSpace(partNumber))
                throw new ArgumentException("型号不能为空", nameof(partNumber));

            var trimmed = partNumber.Trim();
            PartNumber = trimmed;
            PartNumberKey = NormalizeKey(trimmed);
            Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim();
            Source = source;
            OccurrenceCount = 1;
            FirstSeenAt = DateTime.UtcNow;
            LastSeenAt = FirstSeenAt;
        }

        /// <summary>
        /// 生成型号去重键（去首尾空格并转小写）
        /// 线索记录与型号库存在性判定统一使用该口径，避免大小写差异造成重复线索
        /// </summary>
        /// <param name="partNumber">型号原文</param>
        public static string NormalizeKey(string partNumber) => partNumber.Trim().ToLowerInvariant();

        /// <summary>
        /// 标记为已忽略
        /// </summary>
        /// <param name="operatorUserId">执行忽略的管理员用户 ID</param>
        public void Ignore(Guid? operatorUserId)
        {
            IsIgnored = true;
            IgnoredAt = DateTime.UtcNow;
            IgnoredByUserId = operatorUserId;
            UpdateTimestamp();
        }

        /// <summary>
        /// 取消忽略（恢复为待建档）
        /// </summary>
        public void Restore()
        {
            IsIgnored = false;
            IgnoredAt = null;
            IgnoredByUserId = null;
            UpdateTimestamp();
        }

        /// <summary>
        /// 回填计算态：型号库是否已存在同型号
        /// 仅供仓储查询后调用，不持久化
        /// </summary>
        /// <param name="resolved">是否已入库</param>
        public void SetResolved(bool resolved) => IsResolved = resolved;
    }
}
