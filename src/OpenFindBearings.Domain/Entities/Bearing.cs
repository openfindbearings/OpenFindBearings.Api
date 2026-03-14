using OpenFindBearings.Domain.Common;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 轴承产品 - 聚合根
    /// 代表一个具体的轴承型号，是平台的核心业务实体
    /// </summary>
    public class Bearing : BaseEntity
    {
        /// <summary>
        /// 轴承型号（如 6205、6305等）
        /// </summary>
        public string PartNumber { get; private set; }

        /// <summary>
        /// 产品名称（如 深沟球轴承 6205）
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 产品描述
        /// </summary>
        public string? Description { get; private set; }

        /// <summary>
        /// 尺寸参数（内径、外径、宽度）
        /// 封装为值对象，因为三者总是一起出现
        /// </summary>
        public Dimensions Dimensions { get; private set; }

        /// <summary>
        /// 重量（kg）
        /// </summary>
        public decimal? Weight { get; private set; }

        /// <summary>
        /// 精度等级（P0、P6、P5、P4等）
        /// </summary>
        public string? PrecisionGrade { get; private set; }

        /// <summary>
        /// 材料（GCr15、不锈钢、陶瓷等）
        /// </summary>
        public string? Material { get; private set; }

        /// <summary>
        /// 密封方式（2RS、ZZ、Open等）
        /// </summary>
        public string? SealType { get; private set; }

        /// <summary>
        /// 保持架类型（钢保持架、铜保持架等）
        /// </summary>
        public string? CageType { get; private set; }

        /// <summary>
        /// 性能参数（动载荷、静载荷、极限转速）
        /// 封装为值对象，因为有业务规则关联
        /// </summary>
        public PerformanceParams Performance { get; private set; }

        /// <summary>
        /// 轴承类型ID（深沟球、角接触等）
        /// </summary>
        public Guid BearingTypeId { get; private set; }

        /// <summary>
        /// 轴承类型导航属性
        /// </summary>
        public BearingType? BearingType { get; private set; }

        /// <summary>
        /// 品牌ID
        /// </summary>
        public Guid BrandId { get; private set; }

        /// <summary>
        /// 品牌导航属性
        /// </summary>
        public Brand? Brand { get; private set; }

        /// <summary>
        /// 是否有效（软删除标记）
        /// </summary>
        public bool IsActive { get; private set; } = true;

        /// <summary>
        /// 浏览次数（用于热门产品统计）
        /// </summary>
        public int ViewCount { get; private set; }

        // 导航属性

        /// <summary>
        /// 售卖本轴承的所有商家
        /// </summary>
        public List<Merchant> Merchants { get; set; } = [];
        /// <summary>
        /// 商家轴承关联
        /// </summary>
        public List<MerchantBearing> MerchantBearings { get; set; } = [];

        /// <summary>
        /// 无参构造函数，仅供EF Core使用
        /// </summary>
        private Bearing() { }

        /// <summary>
        /// 创建新的轴承产品
        /// </summary>
        /// <param name="partNumber">轴承型号</param>
        /// <param name="name">产品名称</param>
        /// <param name="dimensions">尺寸参数</param>
        /// <param name="bearingTypeId">轴承类型ID</param>
        /// <param name="brandId">品牌ID</param>
        /// <param name="performance">性能参数（可选）</param>
        /// <param name="weight">重量（可选）</param>
        public Bearing(
            string partNumber,
            string name,
            Dimensions dimensions,
            Guid bearingTypeId,
            Guid brandId,
            PerformanceParams? performance = null,
            decimal? weight = null)
        {
            if (string.IsNullOrWhiteSpace(partNumber))
                throw new ArgumentException("型号不能为空", nameof(partNumber));

            PartNumber = partNumber;
            Name = name;
            Dimensions = dimensions ?? throw new ArgumentNullException(nameof(dimensions));
            Weight = weight;
            Performance = performance ?? new PerformanceParams(null, null, null);
            BearingTypeId = bearingTypeId;
            BrandId = brandId;
        }

        /// <summary>
        /// 更新技术参数
        /// 技术参数可能独立变化，所以单独提供更新方法
        /// </summary>
        /// <param name="precisionGrade">精度等级</param>
        /// <param name="material">材料</param>
        /// <param name="sealType">密封方式</param>
        /// <param name="cageType">保持架类型</param>
        public void UpdateTechnicalSpecs(
            string? precisionGrade,
            string? material,
            string? sealType,
            string? cageType)
        {
            PrecisionGrade = precisionGrade;
            Material = material;
            SealType = sealType;
            CageType = cageType;
            UpdateTimestamp();
        }

        /// <summary>
        /// 更新性能参数
        /// </summary>
        /// <param name="performance">新的性能参数</param>
        public void UpdatePerformance(PerformanceParams performance)
        {
            Performance = performance ?? throw new ArgumentNullException(nameof(performance));
            UpdateTimestamp();
        }

        /// <summary>
        /// 更新基本信息
        /// </summary>
        /// <param name="description">产品描述</param>
        /// <param name="weight">重量</param>
        public void UpdateDetails(string? description, decimal? weight)
        {
            Description = description;
            Weight = weight;
            UpdateTimestamp();
        }

        /// <summary>
        /// 增加浏览次数
        /// 每次查看产品详情时调用
        /// </summary>
        public void IncrementViewCount()
        {
            ViewCount++;
            UpdateTimestamp();
        }

        /// <summary>
        /// 停用产品（软删除）
        /// </summary>
        public void Deactivate() => IsActive = false;

        /// <summary>
        /// 启用产品
        /// </summary>
        public void Activate() => IsActive = true;
    }
}
