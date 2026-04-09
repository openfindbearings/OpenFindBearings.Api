using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 商家管理DTO（管理员用）
    /// </summary>
    public class MerchantManagementDto : MerchantDetailDto
    {
        /// <summary>
        /// 注册IP
        /// </summary>
        public string? RegisterIp { get; set; }

        /// <summary>
        /// 最后活跃时间
        /// </summary>
        public DateTime? LastActiveAt { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? AdminRemark { get; set; }

        /// <summary>
        /// 违规记录
        /// </summary>
        public List<ViolationRecordDto> ViolationRecords { get; set; } = new();
    }

    /// <summary>
    /// 违规记录DTO
    /// </summary>
    public class ViolationRecordDto
    {
        public Guid Id { get; set; }
        public DateTime OccurredAt { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Penalty { get; set; }
    }
}
