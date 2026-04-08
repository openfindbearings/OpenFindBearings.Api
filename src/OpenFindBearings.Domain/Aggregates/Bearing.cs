using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Domain.Aggregates
{
    /// <summary>
    /// 轴承产品聚合根
    /// </summary>
    public class Bearing : BaseEntity
    {
        // ============ 基本属性 ============

        /// <summary>
        /// 现行代号
        /// </summary>
        public string CurrentCode { get; private set; }

        /// <summary>
        /// 曾用代号
        /// </summary>
        public string? FormerCode { get; private set; }

        /// <summary>
        /// 代号来源
        /// </summary>
        public string? CodeSource { get; private set; }

        /// <summary>
        /// 产品名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 产品描述
        /// </summary>
        public string? Description { get; private set; }

        /// <summary>
        /// 轴承类型
        /// </summary>
        public string BearingType { get; private set; }

        /// <summary>
        /// 结构类型
        /// </summary>
        public string? StructureType { get; private set; }

        /// <summary>
        /// 尺寸系列
        /// </summary>
        public string? SizeSeries { get; private set; }

        /// <summary>
        /// 是否为标准轴承
        /// </summary>
        public bool IsStandard { get; private set; }

        /// <summary>
        /// 尺寸参数
        /// </summary>
        public Dimensions Dimensions { get; private set; }

        /// <summary>
        /// 最小倒角尺寸 (mm)
        /// </summary>
        public decimal? ChamferRmin { get; private set; }

        /// <summary>
        /// 最大倒角尺寸 (mm)
        /// </summary>
        public decimal? ChamferRmax { get; private set; }

        /// <summary>
        /// 重量 (kg)
        /// </summary>
        public decimal? Weight { get; private set; }

        // ============ 技术参数 ============

        /// <summary>
        /// 精度等级
        /// </summary>
        public string? PrecisionGrade { get; private set; }

        /// <summary>
        /// 材料
        /// </summary>
        public string? Material { get; private set; }

        /// <summary>
        /// 密封方式
        /// </summary>
        public string? SealType { get; private set; }

        /// <summary>
        /// 保持架类型
        /// </summary>
        public string? CageType { get; private set; }

        /// <summary>
        /// 性能参数
        /// </summary>
        public PerformanceParams? Performance { get; private set; }

        /// <summary>
        /// 产地
        /// </summary>
        public string? OriginCountry { get; private set; }

        /// <summary>
        /// 商标
        /// </summary>
        public string? Trademark { get; private set; }

        /// <summary>
        /// 产品类别
        /// </summary>
        public BearingCategory Category { get; private set; } = BearingCategory.Domestic;

        // ============ 关联属性 ============

        /// <summary>
        /// 轴承类型ID（关联字典表）
        /// </summary>
        public Guid BearingTypeId { get; private set; }

        /// <summary>
        /// 轴承类型导航属性
        /// </summary>
        public BearingType? BearingTypeNavigation { get; private set; }

        /// <summary>
        /// 品牌ID
        /// </summary>
        public Guid BrandId { get; private set; }

        /// <summary>
        /// 品牌导航属性
        /// </summary>
        public Brand? Brand { get; private set; }

        /// <summary>
        /// 商家产品关联
        /// </summary>
        public List<MerchantBearing> MerchantBearings { get; private set; } = [];

        /// <summary>
        /// 替代品关系（作为源）
        /// </summary>
        public List<BearingInterchange> SourceInterchanges { get; private set; } = [];

        /// <summary>
        /// 替代品关系（作为目标）
        /// </summary>
        public List<BearingInterchange> TargetInterchanges { get; private set; } = [];

        /// <summary>
        /// 收藏此轴承的用户
        /// </summary>
        public List<UserBearingFavorite> FavoritedByUsers { get; private set; } = [];

        // ============ 数据追溯字段 ============

        /// <summary>
        /// 数据来源信息
        /// </summary>
        public DataSource? DataSource { get; private set; }

        /// <summary>
        /// 最后校验时间
        /// </summary>
        public DateTime? LastVerifiedAt { get; private set; }

        /// <summary>
        /// 校验人/系统
        /// </summary>
        public string? VerifiedBy { get; private set; }

        /// <summary>
        /// 是否为已验证数据
        /// </summary>
        public bool IsVerified { get; private set; }

        /// <summary>
        /// 数据备注
        /// </summary>
        public string? DataRemark { get; private set; }

        // ============ 统计字段 ============

        /// <summary>
        /// 浏览次数
        /// </summary>
        public int ViewCount { get; private set; }

        /// <summary>
        /// 收藏次数
        /// </summary>
        public int FavoriteCount => FavoritedByUsers.Count;

        // ============ 构造函数 ============

        /// <summary>
        /// 私有构造函数，仅供EF Core使用
        /// </summary>
        private Bearing() 
        {
            Name = string.Empty;
            CurrentCode = string.Empty;
            BearingType = string.Empty;
            Dimensions = null!;
        }

        /// <summary>
        /// 创建标准轴承产品
        /// </summary>
        public Bearing(
            string currentCode,
            string name,
            Guid bearingTypeId,
            string bearingType,
            Dimensions dimensions,
            Guid brandId,
            PerformanceParams? performance = null,
            decimal? weight = null)
        {
            if (string.IsNullOrWhiteSpace(currentCode))
                throw new ArgumentException("现行代号不能为空", nameof(currentCode));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("产品名称不能为空", nameof(name));
            if (string.IsNullOrWhiteSpace(bearingType))
                throw new ArgumentException("轴承类型不能为空", nameof(bearingType));

            CurrentCode = currentCode;
            Name = name;
            BearingTypeId = bearingTypeId;
            BearingType = bearingType;
            Dimensions = dimensions ?? throw new ArgumentNullException(nameof(dimensions));
            BrandId = brandId;
            Performance = performance;
            Weight = weight;
            IsStandard = true;

            AddDomainEvent(new BearingCreatedEvent(Id, CurrentCode, BrandId));
        }

        /// <summary>
        /// 创建非标轴承
        /// </summary>
        public static Bearing CreateNonStandard(
            string currentCode,
            string name,
            Guid bearingTypeId, 
            string bearingType,
            Dimensions dimensions,
            Guid brandId,
            string? structureType = null,
            string? sizeSeries = null,
            PerformanceParams? performance = null,
            decimal? weight = null)
        {
            var bearing = new Bearing(currentCode, name, bearingTypeId, bearingType, dimensions, brandId, performance, weight)
            {
                IsStandard = false,
                StructureType = structureType,
                SizeSeries = sizeSeries ?? "非标"
            };

            return bearing;
        }

        // ============ 公共方法 ============

        /// <summary>
        /// 更新基本信息
        /// </summary>
        public void UpdateDetails(string? description, decimal? weight)
        {
            Description = description;
            Weight = weight;
            AddDomainEvent(new BearingUpdatedEvent(Id, CurrentCode, ["Description", "Weight"]));
            UpdateTimestamp();
        }

        /// <summary>
        /// 更新标识信息
        /// </summary>
        public void UpdateIdentification(
            string? formerCode,
            string? codeSource,
            string? trademark)
        {
            FormerCode = formerCode;
            CodeSource = codeSource;
            Trademark = trademark;
            UpdateTimestamp();
        }

        /// <summary>
        /// 更新尺寸相关参数
        /// </summary>
        public void UpdateDimensionDetails(decimal? chamferRmin, decimal? chamferRmax)
        {
            if (chamferRmin < 0) throw new ArgumentException("倒角尺寸不能为负数", nameof(chamferRmin));
            if (chamferRmax < 0) throw new ArgumentException("倒角尺寸不能为负数", nameof(chamferRmax));

            ChamferRmin = chamferRmin;
            ChamferRmax = chamferRmax;
            UpdateTimestamp();
        }

        /// <summary>
        /// 更新技术参数
        /// </summary>
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
        public void UpdatePerformance(PerformanceParams performance)
        {
            Performance = performance ?? throw new ArgumentNullException(nameof(performance));
            UpdateTimestamp();
        }

        /// <summary>
        /// 设置产地和类别
        /// </summary>
        public void SetOrigin(string? country, BearingCategory category)
        {
            OriginCountry = country;
            Category = category;
            UpdateTimestamp();
        }

        /// <summary>
        /// 设置数据来源
        /// </summary>
        public void SetDataSource(DataSource dataSource)
        {
            DataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            UpdateTimestamp();
        }

        /// <summary>
        /// 标记数据为已验证
        /// </summary>
        public void MarkAsVerified(string? verifiedBy = null)
        {
            IsVerified = true;
            LastVerifiedAt = DateTime.UtcNow;
            VerifiedBy = verifiedBy;
            UpdateTimestamp();
        }

        /// <summary>
        /// 添加数据备注
        /// </summary>
        public void AddDataRemark(string remark)
        {
            if (string.IsNullOrWhiteSpace(remark))
                throw new ArgumentException("备注不能为空", nameof(remark));

            DataRemark = string.IsNullOrEmpty(DataRemark)
                ? remark
                : $"{DataRemark}; {remark}";
            UpdateTimestamp();
        }

        /// <summary>
        /// 增加浏览次数
        /// </summary>
        public void IncrementViewCount(Guid? userId = null, string? sessionId = null)
        {
            ViewCount++;

            AddDomainEvent(new BearingViewedEvent(
                Id,
                CurrentCode,
                userId,
                sessionId,
                ViewCount));

            UpdateTimestamp();
        }

        // ============ 查询方法 ============

        /// <summary>
        /// 判断是否有性能参数
        /// </summary>
        public bool HasPerformanceData => Performance?.HasAnyValue ?? false;

        /// <summary>
        /// 获取完整的产品名称
        /// </summary>
        public string GetFullName() => $"{Brand?.Name} {Name}";

        /// <summary>
        /// 获取尺寸字符串
        /// </summary>
        public string GetDimensionString() => Dimensions.ToString();

        /// <summary>
        /// 获取倒角字符串
        /// </summary>
        public string GetChamferString()
        {
            if (!ChamferRmin.HasValue && !ChamferRmax.HasValue)
                return "无";
            if (ChamferRmin == ChamferRmax)
                return $"{ChamferRmin}mm";
            return $"Rmin={ChamferRmin}mm, Rmax={ChamferRmax}mm";
        }

        /// <summary>
        /// 获取轴承摘要信息
        /// </summary>
        public string GetSummary()
        {
            var parts = new List<string> { CurrentCode, BearingType, Dimensions.ToString() };

            if (!IsStandard)
                parts.Insert(1, "[非标]");

            if (Weight.HasValue)
                parts.Add($"{Weight}kg");

            return string.Join(" | ", parts);
        }

        // ============ 内部方法 ============

        /// <summary>
        /// 添加商家关联
        /// </summary>
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
        /// </summary>
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
