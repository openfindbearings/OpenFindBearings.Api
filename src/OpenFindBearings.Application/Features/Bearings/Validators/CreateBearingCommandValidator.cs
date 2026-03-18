using FluentValidation;
using OpenFindBearings.Application.Features.Bearings.Commands;

namespace OpenFindBearings.Application.Features.Bearings.Validators
{
    /// <summary>
    /// 创建轴承命令验证器
    /// </summary>
    public class CreateBearingCommandValidator : AbstractValidator<CreateBearingCommand>
    {
        public CreateBearingCommandValidator()
        {
            // 型号验证
            RuleFor(x => x.PartNumber)
                .NotEmpty().WithMessage("轴承型号不能为空")
                .MaximumLength(50).WithMessage("型号长度不能超过50个字符")
                .Matches(@"^[A-Z0-9\-]+$").WithMessage("型号只能包含大写字母、数字和连字符");

            // 名称验证
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("产品名称不能为空")
                .MaximumLength(200).WithMessage("名称长度不能超过200个字符");

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

            // 重量验证（如果提供）
            When(x => x.Weight.HasValue, () =>
            {
                RuleFor(x => x.Weight!.Value)
                    .GreaterThan(0).WithMessage("重量必须大于0");
            });

            // 性能参数验证
            When(x => x.DynamicLoadRating.HasValue && x.StaticLoadRating.HasValue, () =>
            {
                RuleFor(x => x.DynamicLoadRating!.Value)
                    .LessThanOrEqualTo(x => x.StaticLoadRating!.Value)
                    .WithMessage("动载荷不能大于静载荷");
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
