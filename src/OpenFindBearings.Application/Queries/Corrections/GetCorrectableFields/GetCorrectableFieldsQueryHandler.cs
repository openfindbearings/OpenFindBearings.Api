using MediatR;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Corrections.GetCorrectableFields
{
    /// <summary>
    /// 可纠错字段查询处理器：读取目标实体当前值并按类型输出字段清单。
    /// 清单集合与 ApproveCorrectionCommandHandler 的 Apply switch 严格对齐——
    /// 审批端能应用的字段才允许用户纠错，杜绝"提交了但采纳也不生效"的假选项
    /// </summary>
    public class GetCorrectableFieldsQueryHandler : IRequestHandler<GetCorrectableFieldsQuery, List<CorrectionFieldOptionDto>>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly IMerchantRepository _merchantRepository;

        public GetCorrectableFieldsQueryHandler(
            IBearingRepository bearingRepository,
            IMerchantRepository merchantRepository)
        {
            _bearingRepository = bearingRepository;
            _merchantRepository = merchantRepository;
        }

        public async Task<List<CorrectionFieldOptionDto>> Handle(GetCorrectableFieldsQuery request, CancellationToken cancellationToken)
        {
            return request.TargetType.ToLower() switch
            {
                "bearing" => await GetBearingFieldsAsync(request.TargetId, cancellationToken),
                "merchant" => await GetMerchantFieldsAsync(request.TargetId, cancellationToken),
                _ => []
            };
        }

        /// <summary>轴承可纠错字段（与审批 Apply switch 的 15 个 case 一致）</summary>
        private async Task<List<CorrectionFieldOptionDto>> GetBearingFieldsAsync(Guid bearingId, CancellationToken ct)
        {
            var bearing = await _bearingRepository.GetByIdAsync(bearingId, ct);
            if (bearing == null) return [];

            static string? S(object? v) => v?.ToString();
            return
            [
                new("oldNumber", "型号（老型号）", bearing.OldNumber),
                new("description", "描述", bearing.Description),
                new("weight", "重量", S(bearing.Weight)),
                new("innerDiameter", "内径", S(bearing.Dimensions.InnerDiameter)),
                new("outerDiameter", "外径", S(bearing.Dimensions.OuterDiameter)),
                new("width", "宽度", S(bearing.Dimensions.Width)),
                new("precisionGrade", "精度等级", bearing.PrecisionGrade),
                new("material", "材质", bearing.Material),
                new("sealType", "密封形式", bearing.SealType),
                new("cageType", "保持架类型", bearing.CageType),
                new("dynamicLoad", "动载荷", S(bearing.Performance?.DynamicLoad)),
                new("staticLoad", "静载荷", S(bearing.Performance?.StaticLoad)),
                new("limitingSpeed", "极限转速", S(bearing.Performance?.LimitingSpeed)),
                new("limitingSpeedGrease", "极限转速（脂润滑）", S(bearing.Performance?.LimitingSpeedGrease)),
                new("limitingSpeedOil", "极限转速（油润滑）", S(bearing.Performance?.LimitingSpeedOil))
            ];
        }

        /// <summary>商家可纠错字段（与审批 Apply switch 的 9 个 case 一致）</summary>
        private async Task<List<CorrectionFieldOptionDto>> GetMerchantFieldsAsync(Guid merchantId, CancellationToken ct)
        {
            var merchant = await _merchantRepository.GetByIdAsync(merchantId, ct);
            if (merchant == null) return [];

            return
            [
                new("name", "商家名称", merchant.Name),
                new("companyName", "公司全称", merchant.CompanyName),
                new("description", "简介", merchant.Description),
                new("businessScope", "经营范围", merchant.BusinessScope),
                new("contactPerson", "联系人", merchant.Contact?.ContactPerson),
                new("phone", "电话", merchant.Contact?.Phone),
                new("mobile", "手机", merchant.Contact?.Mobile),
                new("email", "邮箱", merchant.Contact?.Email),
                new("address", "地址", merchant.Contact?.Address)
            ];
        }
    }
}
