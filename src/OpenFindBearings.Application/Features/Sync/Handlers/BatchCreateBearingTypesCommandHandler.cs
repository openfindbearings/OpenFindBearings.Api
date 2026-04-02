using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Sync.Commands;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Sync.Handlers
{
    /// <summary>
    /// 批量创建轴承类型命令处理器
    /// </summary>
    public class BatchCreateBearingTypesCommandHandler : IRequestHandler<BatchCreateBearingTypesCommand, BatchResult>
    {
        private readonly IBearingTypeRepository _bearingTypeRepository;
        private readonly ILogger<BatchCreateBearingTypesCommandHandler> _logger;

        public BatchCreateBearingTypesCommandHandler(
            IBearingTypeRepository bearingTypeRepository,
            ILogger<BatchCreateBearingTypesCommandHandler> logger)
        {
            _bearingTypeRepository = bearingTypeRepository;
            _logger = logger;
        }

        public async Task<BatchResult> Handle(BatchCreateBearingTypesCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始批量创建轴承类型，数量: {Count}, 模式: {Mode}",
                request.BearingTypes.Count, request.Mode);

            var result = new BatchResult();

            foreach (var typeDto in request.BearingTypes)
            {
                try
                {
                    // 检查类型是否已存在
                    var existingType = await _bearingTypeRepository.GetByCodeAsync(typeDto.Code, cancellationToken);

                    if (existingType != null && request.Mode == SyncMode.Create)
                    {
                        result.AddFailed(typeDto.Code, $"轴承类型已存在: {typeDto.Code}");
                        continue;
                    }

                    if (existingType == null && request.Mode == SyncMode.Update)
                    {
                        result.AddFailed(typeDto.Code, $"轴承类型不存在: {typeDto.Code}");
                        continue;
                    }

                    if (existingType == null)
                    {
                        // 创建新类型
                        var bearingType = new BearingType(
                            typeDto.Code,
                            typeDto.Name,
                            typeDto.Description);

                        await _bearingTypeRepository.AddAsync(bearingType, cancellationToken);
                        result.AddSuccess(typeDto.Code, "created", bearingType.Id);
                    }
                    else if (request.Mode == SyncMode.Update || request.Mode == SyncMode.Upsert)
                    {
                        // 更新现有类型
                        existingType.GetType().GetProperty("Name")?.SetValue(existingType, typeDto.Name);
                        existingType.GetType().GetProperty("Description")?.SetValue(existingType, typeDto.Description);

                        await _bearingTypeRepository.UpdateAsync(existingType, cancellationToken);
                        result.AddSuccess(typeDto.Code, "updated", existingType.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "批量创建轴承类型失败: {Code}", typeDto.Code);
                    result.AddFailed(typeDto.Code, ex.Message);
                }
            }

            _logger.LogInformation("批量创建轴承类型完成，成功: {SuccessCount}, 失败: {FailCount}",
                result.SuccessCount, result.FailCount);

            return result;
        }
    }
}
