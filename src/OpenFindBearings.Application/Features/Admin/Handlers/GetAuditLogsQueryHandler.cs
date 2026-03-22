using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Admin.DTOs;
using OpenFindBearings.Application.Features.Admin.Queries;
using OpenFindBearings.Domain.Common.Models;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;
using OpenFindBearings.Domain.Parameters;

namespace OpenFindBearings.Application.Features.Admin.Handlers
{
    /// <summary>
    /// 获取审核日志查询处理器
    /// </summary>
    public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly IBearingTypeRepository _bearingTypeRepository;
        private readonly ICorrectionRequestRepository _correctionRequestRepository;
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<GetAuditLogsQueryHandler> _logger;

        public GetAuditLogsQueryHandler(
     IAuditLogRepository auditLogRepository,
     IUserRepository userRepository,
     IBearingRepository bearingRepository,
     IMerchantRepository merchantRepository,
     IRoleRepository roleRepository,
     IBrandRepository brandRepository,
     IBearingTypeRepository bearingTypeRepository,
     ICorrectionRequestRepository correctionRequestRepository, 
     IMerchantBearingRepository merchantBearingRepository,     
     ILogger<GetAuditLogsQueryHandler> logger)
        {
            _auditLogRepository = auditLogRepository;
            _userRepository = userRepository;
            _bearingRepository = bearingRepository;
            _merchantRepository = merchantRepository;
            _roleRepository = roleRepository;
            _brandRepository = brandRepository;
            _bearingTypeRepository = bearingTypeRepository;
            _correctionRequestRepository = correctionRequestRepository; 
            _merchantBearingRepository = merchantBearingRepository;    
            _logger = logger;
        }

        public async Task<PagedResult<AuditLogDto>> Handle(
            GetAuditLogsQuery request,
            CancellationToken cancellationToken)
        {
            var searchParams = new AuditLogSearchParams
            {
                Action = request.Action,
                EntityType = request.EntityType,
                OperatorId = request.OperatorId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Page = request.Page,
                PageSize = request.PageSize
            };

            var result = await _auditLogRepository.SearchAsync(searchParams, cancellationToken);

            var items = new List<AuditLogDto>();
            foreach (var log in result.Items)
            {
                try
                {
                    // 获取实体名称
                    string entityName = await GetEntityNameAsync(
                        log.EntityType,
                        log.EntityId,
                        cancellationToken);

                    // 获取操作人名称
                    string operatorName = await GetOperatorNameAsync(log.OperatorId, cancellationToken);

                    items.Add(new AuditLogDto
                    {
                        Id = log.Id,
                        Action = log.Action,
                        EntityType = log.EntityType,
                        EntityId = log.EntityId,
                        EntityName = entityName,
                        OperatorId = log.OperatorId,
                        OperatorName = operatorName,
                        OperatedAt = log.OperatedAt,
                        Details = FormatAuditDetails(log), // 格式化详情
                        Remarks = log.Remarks
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "处理审核日志项失败: {LogId}", log.Id);
                    // 添加一个基本项，避免完全丢失
                    items.Add(new AuditLogDto
                    {
                        Id = log.Id,
                        Action = log.Action,
                        EntityType = log.EntityType,
                        EntityId = log.EntityId,
                        EntityName = log.EntityId.ToString(),
                        OperatorId = log.OperatorId,
                        OperatorName = "未知",
                        OperatedAt = log.OperatedAt,
                        Remarks = log.Remarks
                    });
                }
            }

            return new PagedResult<AuditLogDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        /// <summary>
        /// 获取操作人名称
        /// </summary>
        private async Task<string> GetOperatorNameAsync(Guid operatorId, CancellationToken cancellationToken)
        {
            if (operatorId == Guid.Empty)
                return "系统";

            var operatorUser = await _userRepository.GetByIdAsync(operatorId, cancellationToken);
            if (operatorUser == null)
                return "未知用户";

            return operatorUser.Nickname ?? operatorUser.AuthUserId;
        }

        /// <summary>
        /// 格式化审计详情
        /// </summary>
        private string? FormatAuditDetails(AuditLog log)
        {
            // 根据操作类型和实体类型格式化详情
            // 这里简单返回 AfterData，实际项目中可能需要更友好的格式化
            return log.AfterData;
        }

        /// <summary>
        /// 根据实体类型和ID获取实体名称
        /// </summary>
        private async Task<string> GetEntityNameAsync(
            string entityType,
            Guid entityId,
            CancellationToken cancellationToken)
        {
            if (entityId == Guid.Empty)
                return string.Empty;

            try
            {
                return entityType.ToLower() switch
                {
                    "bearing" => await GetBearingNameAsync(entityId, cancellationToken),
                    "merchant" => await GetMerchantNameAsync(entityId, cancellationToken),
                    "user" => await GetUserNameAsync(entityId, cancellationToken),
                    "role" => await GetRoleNameAsync(entityId, cancellationToken),
                    "brand" => await GetBrandNameAsync(entityId, cancellationToken),
                    "bearingtype" => await GetBearingTypeNameAsync(entityId, cancellationToken),
                    "correctionrequest" => await GetCorrectionRequestNameAsync(entityId, cancellationToken),
                    "merchantbearing" => await GetMerchantBearingNameAsync(entityId, cancellationToken),
                    _ => entityId.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取实体名称失败: {EntityType}/{EntityId}", entityType, entityId);
                return entityId.ToString();
            }
        }

        /// <summary>
        /// 获取轴承名称
        /// </summary>
        private async Task<string> GetBearingNameAsync(Guid id, CancellationToken cancellationToken)
        {
            var bearing = await _bearingRepository.GetByIdAsync(id, cancellationToken);
            if (bearing == null)
                return id.ToString();

            return $"{bearing.PartNumber} - {bearing.Name}";
        }

        /// <summary>
        /// 获取商家名称
        /// </summary>
        private async Task<string> GetMerchantNameAsync(Guid id, CancellationToken cancellationToken)
        {
            var merchant = await _merchantRepository.GetByIdAsync(id, cancellationToken);
            return merchant?.Name ?? id.ToString();
        }

        /// <summary>
        /// 获取用户名称
        /// </summary>
        private async Task<string> GetUserNameAsync(Guid id, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(id, cancellationToken);
            if (user == null)
                return id.ToString();

            return user.Nickname ?? user.AuthUserId;
        }

        /// <summary>
        /// 获取角色名称
        /// </summary>
        private async Task<string> GetRoleNameAsync(Guid id, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetByIdAsync(id, cancellationToken);
            return role?.Name ?? id.ToString();
        }

        /// <summary>
        /// 获取品牌名称
        /// </summary>
        private async Task<string> GetBrandNameAsync(Guid id, CancellationToken cancellationToken)
        {
            var brand = await _brandRepository.GetByIdAsync(id, cancellationToken);
            return brand?.Name ?? id.ToString();
        }

        /// <summary>
        /// 获取轴承类型名称
        /// </summary>
        private async Task<string> GetBearingTypeNameAsync(Guid id, CancellationToken cancellationToken)
        {
            var bearingType = await _bearingTypeRepository.GetByIdAsync(id, cancellationToken);
            return bearingType?.Name ?? id.ToString();
        }

        /// <summary>
        /// 获取纠错请求标识
        /// </summary>
        private async Task<string> GetCorrectionRequestNameAsync(Guid id, CancellationToken cancellationToken)
        {
            var correction = await _correctionRequestRepository.GetByIdAsync(id, cancellationToken);
            if (correction == null)
                return id.ToString();

            // 根据目标类型获取目标名称
            string targetName = await GetEntityNameAsync(
                correction.TargetType,
                correction.TargetId,
                cancellationToken);

            return $"纠错请求 - {correction.GetFieldDisplayName()}: {correction.SuggestedValue} (目标: {targetName})";
        }

        /// <summary>
        /// 获取商家-轴承关联标识
        /// </summary>
        private async Task<string> GetMerchantBearingNameAsync(Guid id, CancellationToken cancellationToken)
        {
            var merchantBearing = await _merchantBearingRepository.GetByIdAsync(id, cancellationToken);
            if (merchantBearing == null)
                return id.ToString();

            string merchantName = "未知商家";
            if (merchantBearing.Merchant != null)
            {
                merchantName = merchantBearing.Merchant.Name;
            }
            else if (merchantBearing.MerchantId != Guid.Empty)
            {
                var merchant = await _merchantRepository.GetByIdAsync(merchantBearing.MerchantId, cancellationToken);
                merchantName = merchant?.Name ?? merchantBearing.MerchantId.ToString();
            }

            string bearingName = "未知轴承";
            if (merchantBearing.Bearing != null)
            {
                bearingName = $"{merchantBearing.Bearing.PartNumber} - {merchantBearing.Bearing.Name}";
            }
            else if (merchantBearing.BearingId != Guid.Empty)
            {
                var bearing = await _bearingRepository.GetByIdAsync(merchantBearing.BearingId, cancellationToken);
                bearingName = bearing != null ? $"{bearing.PartNumber} - {bearing.Name}" : merchantBearing.BearingId.ToString();
            }

            string status = merchantBearing.IsOnSale ? "在售" : "已下架";
            if (merchantBearing.IsPendingApproval)
                status = "待审核";

            return $"商家产品 - {merchantName} 销售 {bearingName} [{status}]";
        }
    }
}
