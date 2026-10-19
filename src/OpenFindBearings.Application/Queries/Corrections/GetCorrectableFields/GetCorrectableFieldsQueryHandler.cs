using MediatR;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Corrections.GetCorrectableFields
{
    /// <summary>
    /// 可纠错字段查询处理器：读取目标实体当前值并按类型输出字段清单。
    /// 清单集合与 ApproveCorrectionCommandHandler 的 Apply switch 严格对齐——
    /// 审批端能应用的字段才允许用户纠错，杜绝"提交了但采纳也不生效"的假选项。
    /// 唯一例外：interchange（替代关系报告，v2.17.0 替代品字典化）为"提交类"字段，
    /// 审批不自动应用——由平台管理员在替代品管理页落地替代关系后再审批
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
            // 改动说明（v2.13.2 标签收敛）：字段 id 与值的清单在此维护，显示名统一取自 CorrectionFieldLabels
            // 改动说明（v2.17.0 替代品字典化）：新增 interchange（替代关系报告）——无当前值（Value=null），
            //   作为"报告替代关系有误 / 建议补充替代型号"的反馈入口；审批不自动应用，由平台在替代品管理页落地
            (string Id, string? Value)[] raw =
            [
                ("interchange", null),
                ("oldNumber", bearing.OldNumber),
                ("description", bearing.Description),
                ("weight", S(bearing.Weight)),
                ("innerDiameter", S(bearing.Dimensions.InnerDiameter)),
                ("outerDiameter", S(bearing.Dimensions.OuterDiameter)),
                ("width", S(bearing.Dimensions.Width)),
                ("precisionGrade", bearing.PrecisionGrade),
                ("material", bearing.Material),
                ("sealType", bearing.SealType),
                ("cageType", bearing.CageType),
                ("dynamicLoad", S(bearing.Performance?.DynamicLoad)),
                ("staticLoad", S(bearing.Performance?.StaticLoad)),
                ("limitingSpeed", S(bearing.Performance?.LimitingSpeed)),
                ("limitingSpeedGrease", S(bearing.Performance?.LimitingSpeedGrease)),
                ("limitingSpeedOil", S(bearing.Performance?.LimitingSpeedOil))
            ];
            return raw.Select(f => new CorrectionFieldOptionDto(f.Id, CorrectionFieldLabels.Label("Bearing", f.Id), f.Value)).ToList();
        }

        /// <summary>商家可纠错字段（与审批 Apply switch 的 9 个 case 一致）</summary>
        private async Task<List<CorrectionFieldOptionDto>> GetMerchantFieldsAsync(Guid merchantId, CancellationToken ct)
        {
            var merchant = await _merchantRepository.GetByIdAsync(merchantId, ct);
            if (merchant == null) return [];

            // 改动说明（v2.13.2 标签收敛）：显示名统一取自 CorrectionFieldLabels
            // 改动说明（v2.18.0 卡级重构）：新增 merchantGrade（卡级申诉）——无当前值（Value=null），
            //   作为商家对卡级结果提出异议的申诉入口；审批不自动改级，由管理员在人工调级口落地
            (string Id, string? Value)[] raw =
            [
                ("merchantGrade", null),
                ("name", merchant.Name),
                ("companyName", merchant.CompanyName),
                ("description", merchant.Description),
                ("businessScope", merchant.BusinessScope),
                ("contactPerson", merchant.Contact?.ContactPerson),
                ("phone", merchant.Contact?.Phone),
                ("mobile", merchant.Contact?.Mobile),
                ("email", merchant.Contact?.Email),
                ("address", merchant.Contact?.Address)
            ];
            return raw.Select(f => new CorrectionFieldOptionDto(f.Id, CorrectionFieldLabels.Label("Merchant", f.Id), f.Value)).ToList();
        }
    }
}
