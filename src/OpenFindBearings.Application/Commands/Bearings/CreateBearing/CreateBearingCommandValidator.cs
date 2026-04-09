using FluentValidation;

namespace OpenFindBearings.Application.Commands.Bearings.CreateBearing
{
    public class CreateBearingCommandValidator : AbstractValidator<CreateBearingCommand>
    {
        public CreateBearingCommandValidator()
        {
            // ✅ 修改：PartNumber → CurrentCode
            RuleFor(x => x.CurrentCode)
                .NotEmpty().WithMessage("轴承型号不能为空")
                .MaximumLength(100).WithMessage("型号长度不能超过100个字符")
                .Matches(@"^[A-Z0-9\-]+$").WithMessage("型号只能包含大写字母、数字和连字符");

            // ✅ 新增：曾用代号验证
            RuleFor(x => x.FormerCode)
                .MaximumLength(100).WithMessage("曾用代号长度不能超过100个字符")
                .When(x => !string.IsNullOrWhiteSpace(x.FormerCode));

            // 名称验证
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("产品名称不能为空")
                .MaximumLength(200).WithMessage("名称长度不能超过200个字符");

            // ✅ 新增：轴承类型名称验证
            RuleFor(x => x.BearingType)
                .NotEmpty().WithMessage("轴承类型名称不能为空")
                .MaximumLength(50).WithMessage("轴承类型名称长度不能超过50个字符");

            // 产地验证
            When(x => !string.IsNullOrWhiteSpace(x.OriginCountry), () =>
            {
                RuleFor(x => x.OriginCountry)
                    .MaximumLength(50).WithMessage("产地长度不能超过50个字符");
            });

            // 尺寸验证
            RuleFor(x => x.InnerDiameter)
                .GreaterThan(0).WithMessage("内径必须大于0");

            RuleFor(x => x.OuterDiameter)
                .GreaterThan(x => x.InnerDiameter)
                .WithMessage("外径必须大于内径");

            RuleFor(x => x.Width)
                .GreaterThan(0).WithMessage("宽度必须大于0");

            // 关联ID验证
            RuleFor(x => x.BearingTypeId)
                .NotEmpty().WithMessage("轴承类型不能为空");

            RuleFor(x => x.BrandId)
                .NotEmpty().WithMessage("品牌不能为空");

            // 重量验证
            When(x => x.Weight.HasValue, () =>
            {
                RuleFor(x => x.Weight!.Value)
                    .GreaterThan(0).WithMessage("重量必须大于0");
            });

            // 倒角验证
            When(x => x.ChamferRmin.HasValue, () =>
            {
                RuleFor(x => x.ChamferRmin!.Value)
                    .GreaterThanOrEqualTo(0).WithMessage("倒角尺寸不能为负数");
            });

            When(x => x.ChamferRmax.HasValue, () =>
            {
                RuleFor(x => x.ChamferRmax!.Value)
                    .GreaterThanOrEqualTo(0).WithMessage("倒角尺寸不能为负数");
            });

            // 性能参数验证
            When(x => x.DynamicLoadRating.HasValue && x.StaticLoadRating.HasValue, () =>
            {
                RuleFor(x => x.DynamicLoadRating!.Value)
                    .LessThanOrEqualTo(x => x.StaticLoadRating!.Value * 1.5m)
                    .WithMessage("动载荷异常大于静载荷，请核对数据");
            });

            // 精度等级格式验证
            When(x => !string.IsNullOrWhiteSpace(x.PrecisionGrade), () =>
            {
                RuleFor(x => x.PrecisionGrade)
                    .Matches(@"^P[0-6]$|^[0-9]$")
                    .WithMessage("精度等级格式不正确（如 P0, P6, 5 等）");
            });
        }
    }
}
