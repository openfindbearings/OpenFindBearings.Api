using FluentValidation;
using OpenFindBearings.Application.Features.Bearings.Commands;

namespace OpenFindBearings.Application.Features.Bearings.Validators
{
    /// <summary>
    /// 更新轴承命令验证器
    /// </summary>
    public class UpdateBearingCommandValidator : AbstractValidator<UpdateBearingCommand>
    {
        public UpdateBearingCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("轴承ID不能为空");

            When(x => x.Name != null, () =>
            {
                RuleFor(x => x.Name)
                    .MaximumLength(200).WithMessage("名称长度不能超过200个字符");
            });

            // 添加产地验证
            When(x => !string.IsNullOrWhiteSpace(x.OriginCountry), () =>
            {
                RuleFor(x => x.OriginCountry)
                    .MaximumLength(50).WithMessage("产地长度不能超过50个字符");
            });

            When(x => x.Weight.HasValue, () =>
            {
                RuleFor(x => x.Weight!.Value)
                    .GreaterThan(0).WithMessage("重量必须大于0");
            });

            When(x => x.PrecisionGrade != null, () =>
            {
                RuleFor(x => x.PrecisionGrade)
                    .Matches(@"^P[0-6]$|^[0-9]$")
                    .WithMessage("精度等级格式不正确（如 P0, P6, 5 等）");
            });
        }
    }
}
