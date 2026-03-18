using OpenFindBearings.Domain.Common;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 轴承产品聚合根
    /// 代表一个具体的轴承型号，是平台的核心业务实体
    /// 对应接口：GET /api/bearings/search, GET /api/bearings/{id}, GET /api/bearings/hot
    /// </summary>
    public class Bearing : BaseEntity
    {
        // ============ 基本属性 ============

        /// <summary>
        /// 轴承型号（如 6205、6305等）
        /// 全球通用的轴承型号标识，具有唯一性
        /// </summary>
        public string PartNumber { get; private set; }

        /// <summary>
        /// 产品名称（如 深沟球轴承 6205）
        /// 用于展示的产品名称，通常包含品牌和型号
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 产品描述
        /// 详细的轴承描述信息，包括特点、用途等
        /// </summary>
        public string? Description { get; private set; }

        /// <summary>
        /// 尺寸参数（值对象）
        /// 包含内径、外径、宽度三个核心尺寸
        /// </summary>
        public Dimensions Dimensions { get; private set; }

        /// <summary>
        /// 重量 (kg)
        /// 单个轴承的重量，用于物流和成本估算
        /// </summary>
        public decimal? Weight { get; private set; }

        // ============ 技术参数 ============

        /// <summary>
        /// 精度等级（P0、P6、P5、P4等）
        /// 按照ISO标准定义的轴承精度等级
        /// </summary>
        public string? PrecisionGrade { get; private set; }

        /// <summary>
        /// 材料（GCr15、不锈钢、陶瓷等）
        /// 轴承套圈和滚动体的材料
        /// </summary>
        public string? Material { get; private set; }

        /// <summary>
        /// 密封方式（2RS、ZZ、Open等）
        /// 2RS: 双面橡胶密封
        /// ZZ: 双面金属防尘盖
        /// Open: 开放式
        /// </summary>
        public string? SealType { get; private set; }

        /// <summary>
        /// 保持架类型（钢保持架、铜保持架、尼龙保持架等）
        /// 保持架的材料和结构类型
        /// </summary>
        public string? CageType { get; private set; }

        /// <summary>
        /// 性能参数（值对象）
        /// 包含动载荷、静载荷、极限转速等性能指标
        /// </summary>
        public PerformanceParams? Performance { get; private set; }

        // ============ 关联属性 ============

        /// <summary>
        /// 轴承类型ID
        /// 关联到轴承类型字典表
        /// </summary>
        public Guid BearingTypeId { get; private set; }

        /// <summary>
        /// 轴承类型导航属性
        /// </summary>
        public BearingType? BearingType { get; private set; }

        /// <summary>
        /// 品牌ID
        /// 关联到品牌字典表
        /// </summary>
        public Guid BrandId { get; private set; }

        /// <summary>
        /// 品牌导航属性
        /// </summary>
        public Brand? Brand { get; private set; }

        /// <summary>
        /// 商家产品关联导航属性
        /// 此轴承被哪些商家销售，一对多关系
        /// </summary>
        public List<MerchantBearing> MerchantBearings { get; private set; } = [];

        /// <summary>
        /// 替代品关系导航属性（作为源轴承）
        /// 此轴承可以替代哪些其他轴承
        /// </summary>
        public List<BearingInterchange> SourceInterchanges { get; private set; } = [];

        /// <summary>
        /// 替代品关系导航属性（作为目标轴承）
        /// 哪些其他轴承可以替代此轴承
        /// </summary>
        public List<BearingInterchange> TargetInterchanges { get; private set; } = [];

        /// <summary>
        /// 收藏此轴承的用户
        /// 多对多关系，通过UserFavorite中间表关联
        /// </summary>
        public List<UserBearingFavorite> FavoritedByUsers { get; private set; } = [];

        // ============ 统计字段 ============

        /// <summary>
        /// 浏览次数
        /// 记录轴承被查看的次数，用于热门产品排序
        /// </summary>
        public int ViewCount { get; private set; }

        /// <summary>
        /// 收藏次数
        /// 计算属性，返回收藏此轴承的用户数量
        /// </summary>
        public int FavoriteCount => FavoritedByUsers.Count;

        /// <summary>
        /// 私有构造函数，仅供EF Core使用
        /// </summary>
        private Bearing() { }

        /// <summary>
        /// 创建轴承产品
        /// </summary>
        /// <param name="partNumber">轴承型号，不能为空</param>
        /// <param name="name">产品名称，不能为空</param>
        /// <param name="dimensions">尺寸参数，不能为空</param>
        /// <param name="bearingTypeId">轴承类型ID</param>
        /// <param name="brandId">品牌ID</param>
        /// <param name="performance">性能参数（可选）</param>
        /// <param name="weight">重量（可选）</param>
        /// <exception cref="ArgumentException">当必要参数为空时抛出</exception>
        /// <exception cref="ArgumentNullException">当值对象为空时抛出</exception>
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
            BearingTypeId = bearingTypeId;
            BrandId = brandId;
            Performance = performance;
            Weight = weight;
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
        /// <param name="performance">新的性能参数值对象</param>
        /// <exception cref="ArgumentNullException">当performance为空时抛出</exception>
        public void UpdatePerformance(PerformanceParams performance)
        {
            Performance = performance ?? throw new ArgumentNullException(nameof(performance));
            UpdateTimestamp();
        }

        /// <summary>
        /// 增加浏览次数
        /// 每次用户查看轴承详情时调用，触发轴承被查看的领域事件
        /// </summary>
        /// <param name="userId">查看的用户ID（如果是登录用户）</param>
        /// <param name="sessionId">游客会话ID（如果是未登录用户）</param>
        public void IncrementViewCount(Guid? userId = null, string? sessionId = null)
        {
            ViewCount++;

            // 触发轴承被查看的领域事件，用于统计和推荐
            AddDomainEvent(new BearingViewedEvent(
                Id,
                PartNumber,
                userId,
                sessionId,
                ViewCount));

            UpdateTimestamp();
        }

        /// <summary>
        /// 判断是否有性能参数
        /// </summary>
        /// <returns>如果有任何性能参数（动载荷、静载荷、转速）则返回true</returns>
        public bool HasPerformanceData => Performance?.HasAnyValue ?? false;

        /// <summary>
        /// 获取完整的产品名称（包含品牌）
        /// </summary>
        /// <returns>格式如 "SKF 深沟球轴承 6205"</returns>
        public string GetFullName() => $"{Brand?.Name} {Name}";

        /// <summary>
        /// 添加商家关联
        /// 当商家将此轴承添加到店铺时调用
        /// </summary>
        /// <param name="merchantBearing">商家-轴承关联实体</param>
        /// <exception cref="ArgumentNullException">当merchantBearing为空时抛出</exception>
        internal void AddMerchantBearing(MerchantBearing merchantBearing)
        {
            if (merchantBearing == null)
                throw new ArgumentNullException(nameof(merchantBearing));

            if (!MerchantBearings.Any(mb => mb.BearingId == merchantBearing.BearingId))
            {
                MerchantBearings.Add(merchantBearing);
                UpdateTimestamp();
            }
        }

        /// <summary>
        /// 移除商家关联
        /// 当商家从店铺移除此轴承时调用
        /// </summary>
        /// <param name="bearingId">轴承ID</param>
        internal void RemoveMerchantBearing(Guid bearingId)
        {
            var mb = MerchantBearings.FirstOrDefault(x => x.BearingId == bearingId);
            if (mb != null)
            {
                MerchantBearings.Remove(mb);
                UpdateTimestamp();
            }
        }
    }
}
