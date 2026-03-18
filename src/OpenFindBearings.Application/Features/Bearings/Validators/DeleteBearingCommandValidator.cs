using FluentValidation;
using OpenFindBearings.Application.Features.Bearings.Commands;

namespace OpenFindBearings.Application.Features.Bearings.Validators
{
    /// <summary>
    /// 删除轴承命令验证器
    /// </summary>
    public class DeleteBearingCommandValidator : AbstractValidator<DeleteBearingCommand>
    {
        public DeleteBearingCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("轴承ID不能为空");
        }
    }
}
