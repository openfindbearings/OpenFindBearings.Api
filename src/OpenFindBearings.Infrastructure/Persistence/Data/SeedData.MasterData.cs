using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Infrastructure.Persistence.Data
{
    /// <summary>
    /// 主数据种子：品牌、轴承类型（型号类别）与商家粉丝数校准。
    /// 改动说明：这些数据此前由多个迁移（BackfillBrandAndTypeProfiles、BackfillMerchantFollowerCount）写入，
    ///   迁移收缩后统一收敛到本文件；一律按业务键判空补全，不覆盖后台人工维护的内容。
    /// </summary>
    public static partial class SeedData
    {
        /// <summary>品牌基础档案定义（新建品牌时写入）。</summary>
        private sealed record BrandSeed(
            string Code,
            string Name,
            BrandLevel Level,
            string Country,
            string Description,
            int? FoundedYear,
            string? ParentCompany,
            string? Website,
            string? Advantages,
            string? MainApplications);

        /// <summary>已有品牌的档案补丁（仅当该品牌已存在且简介为空时写入）。</summary>
        private sealed record BrandProfilePatch(
            string Code,
            string Description,
            int? FoundedYear,
            string Country,
            string? ParentCompany,
            string? Website,
            string? Advantages,
            string? SuffixNotes,
            string? MainApplications);

        /// <summary>轴承类型基础档案定义。</summary>
        private sealed record BearingTypeSeed(
            string Code,
            string Name,
            string Description,
            string EnglishName,
            string StructureFeatures,
            string LoadCharacteristics,
            string TypicalPrefixes,
            string MainApplications,
            bool IsSeparable);

        /// <summary>
        /// 新增品牌清单：补齐主流国际与国内有明确编码的品牌，编码不确定者留给后续数据源补录。
        /// 全部标记为 Manual 且 ImportedBy=seed-brand-profile，同步任务不会覆盖。
        /// </summary>
        private static readonly BrandSeed[] BrandSeeds =
        [
            new("NMB", "美蓓亚三美", BrandLevel.InternationalStandard, "日本", "日本美蓓亚三美（MinebeaMitsumi）旗下轴承品牌，微型与小型精密轴承全球领先，电机与汽车电子配套为主。", 1951, "MinebeaMitsumi 集团", "https://www.nmbtc.com", "微型深沟球轴承、电机轴承、精密小型轴承", "微特电机、汽车电子、家电、办公设备"),
            new("FYH", "FYH", BrandLevel.InternationalStandard, "日本", "日本带座外球面轴承单元专业厂，UC/UK 系列外球面轴承与铸铁座体成套供应，农机与输送线常用。", 1950, "FYH 株式会社", "https://www.fyh.co.jp", "带座外球面轴承单元、外球面轴承芯", "输送机械、农机、纺织、食品机械"),
            new("ASAHI", "旭精工", BrandLevel.InternationalStandard, "日本", "日本旭精工（ASAHI SEIKO）带座轴承品牌，外球面轴承单元系列齐全，注重密封与免维护。", 1956, "ASAHI SEIKO 株式会社", null, "带座外球面轴承单元、不锈钢轴承单元", "输送线、食品与包装机械、农机"),
            new("EZO", "EZO", BrandLevel.InternationalStandard, "日本", "日本微型轴承品牌，以不锈钢与深沟球微型轴承见长，常用于小电机与精密仪器。", 1950, "EZO 株式会社", null, "微型不锈钢轴承、薄壁轴承", "微特电机、仪器仪表、办公设备、模型"),
            new("KBC", "KBC", BrandLevel.InternationalStandard, "韩国", "韩国通用轴承品牌，规格覆盖深沟球、圆锥滚子与带座单元，性价比路线。", 1969, "KBC 株式会社", null, "深沟球轴承、圆锥滚子轴承、带座轴承单元", "电机、汽车、农机、通用机械"),
            new("ZKL", "ZKL", BrandLevel.InternationalStandard, "捷克", "捷克老牌轴承集团，欧洲本土制造，通用轴承与铁路、冶金配套经验丰富。", 1926, "ZKL Group", "https://www.zkl.cz", "深沟球轴承、调心滚子轴承、圆锥滚子轴承、铁路轴承", "铁路、冶金、矿山、通用机械"),
            new("FKL", "FKL", BrandLevel.Economy, "塞尔维亚", "塞尔维亚轴承制造商，主产通用工业轴承与农机轴承，欧洲二线性价比品牌。", 1963, "FKL 集团", null, "深沟球轴承、圆锥滚子轴承、农机轴承、带座单元", "农机、矿山、输送、通用机械"),
            new("URB", "URB", BrandLevel.Economy, "罗马尼亚", "罗马尼亚大型轴承集团，重载与大型轴承见长，广泛用于轧机、矿山与能源装备。", 1953, "URB 集团", null, "调心滚子轴承、圆柱滚子轴承、圆锥滚子轴承、大型轴承", "冶金轧机、矿山、水泥、能源装备"),
            new("NKE", "NKE", BrandLevel.InternationalStandard, "奥地利", "奥地利工业轴承品牌，专注标准工业轴承与定制方案，欧洲中端定位。", 1996, "NKE Austria GmbH", "https://www.nke.at", "深沟球轴承、调心滚子轴承、圆柱滚子轴承、轴承座", "风电、冶金、输送、通用机械"),
            new("RHP", "RHP", BrandLevel.InternationalStandard, "英国", "英国老牌轴承品牌，现属 NSK 集团，精密与通用轴承并重，欧洲市场认知度高。", 1899, "NSK 集团", "https://www.nsk.com", "深沟球轴承、角接触球轴承、带座单元", "电机、机床、纺织、通用机械"),
            new("Barden", "Barden", BrandLevel.InternationalPremium, "美国", "美国精密轴承品牌，现属舍弗勒集团，以高精度角接触球轴承服务机床主轴与航空。", 1907, "舍弗勒集团", "https://www.schaeffler.com", "精密角接触球轴承、主轴轴承", "机床主轴、航空、医疗、真空泵"),
            new("GMN", "GMN", BrandLevel.InternationalPremium, "德国", "德国家族轴承企业，精密主轴轴承与高速电主轴并重，机床与涡轮领域口碑稳定。", 1908, "GMN Paul Müller Industrie GmbH", "https://www.gmn.de", "精密角接触球轴承、主轴单元、单向离合器", "机床主轴、涡轮机械、真空泵"),
            new("CXD", "慈兴", BrandLevel.DomesticPremium, "中国", "宁波慈兴集团，微小型深沟球轴承与汽车轴承规模领先，主攻电机与汽车配套市场。", 1984, "慈兴集团有限公司", "https://www.cxbearing.com", "微小型深沟球轴承、汽车轴承、电机轴承", "微特电机、汽车、家电、电动工具"),
            new("QC", "万向钱潮", BrandLevel.DomesticPremium, "中国", "万向集团旗下汽车零部件上市公司，轴承业务覆盖轮毂单元、圆锥滚子与传动轴配套。", 1994, "万向集团", "https://www.wxqc.com.cn", "汽车轮毂轴承单元、圆锥滚子轴承、传动轴", "汽车整车配套、底盘与传动系统"),
            new("NF", "南方轴承", BrandLevel.DomesticStandard, "中国", "常州南方轴承，滚针轴承与单向离合器专业厂，汽车与摩托车配套为主。", 1988, "江苏南方精工股份有限公司", null, "滚针轴承、单向离合器、汽车张紧轮轴承", "汽车变速器、摩托车、农机、电动工具"),
        ];

        /// <summary>
        /// 已有品牌档案补丁：这些编码来自爬取数据源，新库不会凭空创建，只在已存在且简介为空时补档案。
        /// </summary>
        private static readonly BrandProfilePatch[] BrandProfilePatches =
        [
            new("CJB", "重庆机电控股集团控股的国有股份制轴承企业，以汽车与摩托车轴承为主，主打低噪音密封深沟球轴承与轮毂轴承单元。", 1990, "中国", "重庆机电控股（集团）公司", "https://www.cjb.com.cn", "低噪音密封深沟球轴承、汽车轮毂单元、四点接触球轴承、圆柱滚子轴承、精密锻件", null, "汽车变速器与底盘、摩托车、机床、工程机械、电机"),
            new("FYB", "由镇江轴承厂改制而来的滚针轴承专业厂，国家数控机床国产化滚针轴承主要研制单位，江苏省高新技术企业。", 1970, "中国", null, "http://www.fyb-bearing.cn", "滚针轴承、圆柱滚子轴承、推力轴承、角接触球轴承、复合轴承", null, "汽车、冶金、机床、纺织、印刷、轻工、工程机械"),
            new("HCH", "浙江慈溪民营轴承集团，1973 年创立，深沟球轴承出口规模居国内前列，近年重点切入机器人谐波减速器配套轴承。", 1973, "中国", "环驰轴承集团（HCH Bearing Group）", "https://www.hchbearing.com", "深沟球轴承、柔性薄壁轴承、交叉滚子轴承、四点接触球轴承、微型轴承", "ZZ=铁盖防尘；2RS=接触密封", "家电、电机、汽车、机器人、工业自动化"),
            new("HOGO", "山东临清推力轴承制造企业，主产推力球轴承、推力滚子轴承与调心滚子轴承，年产能三百万套以上。", null, "中国", "山东华钢精密轴承制造有限公司", "http://www.hogozc.com", "推力球轴承、推力圆柱滚子轴承、推力圆锥滚子轴承、推力调心滚子轴承、调心滚子轴承", "M=黄铜实体保持架；W33=外圈油槽与油孔", "齿轮装置、汽车、机床、注塑机、起重运输、矿山工程机械"),
            new("PEER", "美国轴承品牌，1941 年起步，2008 年被 SKF 收购后作为独立品牌运营，聚焦农机、电机与物料搬运等目标行业。", 1941, "美国", "SKF 集团", "https://www.peerbearing.com", "深沟球轴承、带座轴承单元、农机轴承、圆锥滚子轴承", null, "农业机械、电机、流体设备、工业传动、物料搬运、非公路车辆"),
            new("UBC", "优必胜（上海）精密轴承，2000 年注册 UBC 商标，2017 年成为日本 IKO 集团全资子公司，苏州设厂并沿用日系品质管控。", 2000, "中国", "日本 IKO 集团（Nippon Thompson）", "https://www.ubc-bearing.com", "圆柱滚子轴承、圆锥滚子轴承、调心滚子轴承、带座轴承、直线导轨与滚珠丝杠", null, "压缩机、风电、机器人 RV 减速器、电梯、工业齿轮箱、农机、矿山冶金"),
            new("YHD", "东莞怡合达自动化（深交所创业板 301029），FA 工厂自动化零部件一站式供应平台，轴承为其传动零部件品类之一。", 2010, "中国", "东莞怡合达自动化股份有限公司", "https://www.yhdfa.com", "FA 零部件一站式供应、标准化选型与 3D 模型、短交期", null, "自动化设备、锂电与光伏装备、3C 制造、机床"),
            new("ZXY", "1968 年建厂的国有汽车轴承基地，1996 年深交所上市（000678），现由湖北三环集团控股，2013 年收购波兰 KFLT。", 1968, "中国", "湖北三环集团有限公司", "https://www.zxy.com.cn", "汽车轮毂轴承单元、圆锥滚子轴承、圆柱滚子轴承、滚针轴承、等速万向节、离合器分离轴承", null, "商用车与乘用车整车配套、农机、风电、工程机械"),
            new("ZYS", "前身洛阳轴承研究所（1958 年成立），国机集团下属高新技术企业，专攻高精尖特专轴承，航空航天与机床为主战场。", 1958, "中国", "中国机械工业集团有限公司（国机集团）", "https://www.zys.com.cn", "精密与特种轴承、电主轴、轴承专用装备与检测仪器", null, "航空航天、舰船兵器、机床工具、风电、矿山冶金、石油化工、医疗器械、轨道交通"),
        ];

        /// <summary>轴承类型基础档案清单（编码语义以库内中文名为准：SRB 为调心球轴承、SRLB 为调心滚子轴承）。</summary>
        private static readonly BearingTypeSeed[] BearingTypeSeeds =
        [
            new("DGBB", "深沟球轴承", "最常用的滚动轴承，主要承受径向载荷", "Deep Groove Ball Bearing", "内外圈各一道深沟槽，钢球与沟槽四点接触，结构简单、摩擦小、易维护", "以径向载荷为主，可承受一定双向轴向载荷", "60/62/63/64/68/69", "电机、家电、汽车、通用机械、精密仪器", false),
            new("ACBB", "角接触球轴承", "可同时承受径向和轴向载荷", "Angular Contact Ball Bearing", "内外圈沟槽存在接触角，单列通常需成对安装（面对面、背对背或串联）", "径向与单向轴向联合承载，接触角越大轴向能力越强", "70/72/73/74/79/32/33", "机床主轴、减速机、泵、空压机、高频电机", false),
            new("SRB", "调心球轴承", "外圈球面滚道配双列钢球，可自动补偿同轴度误差", "Self-Aligning Ball Bearing", "外圈球面滚道配双列钢球，允许内外圈相对偏转，自动补偿同轴度误差", "以径向载荷为主，可承受少量轴向载荷，抗挠曲能力强", "12/13/22/23", "纺织机械、农机、输送设备、木工机械", false),
            new("SRLB", "调心滚子轴承", "双列鼓形滚子，承载能力高于调心球轴承", "Spherical Roller Bearing", "外圈球面滚道配双列鼓形滚子，允许内外圈相对偏转，承载能力高于调心球轴承", "径向载荷为主，可承受双向轴向载荷，抗冲击与过载能力强", "213/222/223/230/231/232/239/240/241", "冶金轧机、矿山、风机、造纸、水泥", false),
            new("CRB", "圆柱滚子轴承", "滚子与滚道线接触，径向承载能力大、刚性好", "Cylindrical Roller Bearing", "滚子与滚道线接触，套圈带挡边，多数型号内外圈可分离，便于装拆", "承受较大径向载荷，刚性好、转速高，一般不承受轴向载荷", "N/NU/NJ/NUP/NN/NNU 配合 2/3/4 尺寸系列", "电机、轧机、齿轮箱、机床主轴、压缩机", true),
            new("TRB", "圆锥滚子轴承", "锥形滚子线接触，可承受径向与单向轴向联合载荷", "Tapered Roller Bearing", "锥形滚子与锥形滚道线接触，各锥面汇交于轴心线同一点，多成对安装使用", "承受径向与单向轴向联合载荷，刚性高，单列需配对消除游隙", "302/303/313/320/322/323/330/331/332", "汽车车桥与变速器、齿轮箱、轧机、工程机械", true),
            new("NRB", "滚针轴承", "细长滚针作滚动体，径向截面小、承载能力大", "Needle Roller Bearing", "细长滚针作滚动体，径向截面小，分带内圈、无内圈与冲压外圈等形式", "径向承载能力大而占用径向空间小，不承受轴向载荷", "HK/BK/NA/NK/RNA/NAV/NKI", "汽车变速器、万向节、液压泵、纺织机械", true),
            new("TBB", "推力球轴承", "仅承受轴向载荷，适用于低速重载轴向定位", "Thrust Ball Bearing", "轴圈、座圈与钢球组合，单向型承受单向轴向力，双向型两套并列布置", "仅承受轴向载荷，不能承受径向载荷", "511/512/513/514/522/523/532/533/542/543", "起重机吊钩、立式泵、机床、减速机", true),
            new("THRB", "推力滚子轴承", "推力圆柱、推力圆锥与推力调心滚子三类，承受很大轴向载荷", "Thrust Roller Bearing", "滚子轴线与轴心线垂直布置，含推力圆柱、推力圆锥与推力调心滚子三类", "承受很大的单向或双向轴向载荷，刚性高，不能承受径向载荷", "811/812/813/893/894/292/293/294", "轧机压下机构、船舶推进、挤出机、立式电机", true),
            new("LB", "直线轴承", "外筒内装钢球沿光轴往复直线运动", "Linear Ball Bearing", "外筒内装钢球与保持架，沿光轴往复直线运动，分标准型、法兰型与开口型", "承受径向载荷，摩擦小、运动平稳，不适合承受旋转载荷", "LM/LME/LMB/LMK/LMH/LMF/KH", "3D 打印机、自动化模组、包装机械、测量设备", false),
            new("SPB", "关节轴承", "内圈外球面与外圈内球面滑动接触，可多方向偏转", "Spherical Plain Bearing", "内圈外球面与外圈内球面滑动接触，可承受多方向偏转与摆动", "承受径向与任意方向轴向联合载荷，适合摆动与低速转动", "GE/GEG/GEE/SI/SA/PHS", "工程机械油缸、液压支架、转向拉杆、机器人关节", true),
            new("SWB", "回转支承", "大直径内外圈集成滚道与密封，单套实现回转支撑", "Slewing Bearing", "大直径内外圈带齿或安装孔，集成滚道与密封，单套实现回转支撑", "同时承受轴向、径向与倾覆力矩，以低速重载工况为主", "01/02/11/13/22 系列", "挖掘机、起重机、风电变桨、雷达与光伏跟踪", true),
            new("HUB", "带座轴承单元", "外球面轴承装入座体成套供应，可自动调心", "Mounted Bearing Unit", "外球面轴承装入铸铁或冲压座体，座体带安装孔，可自动调心", "以径向载荷为主，可承受中等轴向载荷，密封性好适合粉尘环境", "UCP/UCF/UCFL/UCPH/UCPA/UCFC/UCT", "输送机、农机、纺织印染、矿山、环保设备", false),
            new("IB", "外球面球轴承", "深沟球轴承变型，外径为球面并带顶丝或偏心套", "Insert Ball Bearing", "深沟球轴承变型，外圈外径为球面，内圈加长并带顶丝或偏心套", "以径向载荷为主，可承受径向与轴向联合载荷，不宜单独承受轴向力", "UC/UE/SA/SB/UK/UD", "带座单元配套、农机、输送线、食品机械", false),
            new("CMB", "组合轴承", "径向轴承与推力轴承集成为一套，结构紧凑", "Combination Bearing", "径向轴承与推力轴承集成为一套，共用内圈或外圈，结构紧凑", "同时承受径向与轴向载荷，安装与对中要求较高", "NKX/NKXR/RAX/ZARF/ZARN", "丝杠支撑、减速机、泵、机床进给系统", true),
            new("TKB", "滚轮轴承", "外圈为加厚滚动面，含凸轮从动件与支承滚轮", "Track Roller Bearing", "外圈为加厚滚动面，内圈或螺栓轴带安装结构，含凸轮从动件与支承滚轮", "承受径向载荷与一定冲击，外圈直接作为滚道在导轨上滚动", "NUTR/NUKR/NATR/NAST/STO/CF", "凸轮机构、输送导轨、自动化滑台、包装机械", false),
        ];

        /// <summary>
        /// 幂等补全品牌与轴承类型档案，并校准商家粉丝数。
        /// 品牌按 Code 判重、类型按 Code 判重；已有记录只在对应档案字段为空时补写，绝不覆盖人工内容。
        /// </summary>
        private static async Task EnsureMasterDataAsync(ApplicationDbContext context, ILogger logger)
        {
            var brandAdded = 0;
            var brandPatched = 0;
            var typeAdded = 0;
            var typePatched = 0;

            var existingBrands = await context.Brands.ToListAsync();
            var brandsByCode = existingBrands
                .GroupBy(b => b.Code, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var seed in BrandSeeds)
            {
                if (brandsByCode.ContainsKey(seed.Code))
                {
                    continue;
                }

                var brand = new Brand(seed.Code, seed.Name, seed.Level);
                brand.UpdateProfile(
                    seed.Description,
                    seed.FoundedYear,
                    seed.ParentCompany,
                    seed.Website,
                    seed.Advantages,
                    null,
                    seed.MainApplications);
                brand.SetDataSource(DataSource.FromManual("seed-brand-profile"));
                context.Brands.Add(brand);
                brandsByCode[seed.Code] = brand;
                brandAdded++;
            }

            foreach (var patch in BrandProfilePatches)
            {
                if (!brandsByCode.TryGetValue(patch.Code, out var brand) || brand.Description is not null)
                {
                    continue;
                }

                brand.UpdateProfile(
                    patch.Description,
                    patch.FoundedYear,
                    patch.ParentCompany,
                    patch.Website,
                    patch.Advantages,
                    patch.SuffixNotes,
                    patch.MainApplications);
                brand.SetDataSource(DataSource.FromManual("seed-brand-profile"));
                brandPatched++;
            }

            var existingTypes = await context.BearingTypes.ToListAsync();
            var typesByCode = existingTypes
                .GroupBy(t => t.Code, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var seed in BearingTypeSeeds)
            {
                if (!typesByCode.TryGetValue(seed.Code, out var bearingType))
                {
                    bearingType = new BearingType(seed.Code, seed.Name, seed.Description);
                    bearingType.UpdateProfile(
                        seed.EnglishName,
                        seed.StructureFeatures,
                        seed.LoadCharacteristics,
                        seed.TypicalPrefixes,
                        seed.MainApplications,
                        seed.IsSeparable,
                        null);
                    bearingType.SetDataSource(DataSource.FromManual("seed-brand-profile"));
                    context.BearingTypes.Add(bearingType);
                    typesByCode[seed.Code] = bearingType;
                    typeAdded++;
                    continue;
                }

                if (bearingType.EnglishName is not null)
                {
                    continue;
                }

                bearingType.UpdateProfile(
                    seed.EnglishName,
                    seed.StructureFeatures,
                    seed.LoadCharacteristics,
                    seed.TypicalPrefixes,
                    seed.MainApplications,
                    seed.IsSeparable,
                    null);
                bearingType.SetDataSource(DataSource.FromManual("seed-brand-profile"));
                typePatched++;
            }

            if (brandAdded + brandPatched + typeAdded + typePatched > 0)
            {
                await context.SaveChangesAsync();
                logger.LogInformation(
                    "主数据已补全：品牌新增 {BrandAdded}、品牌档案补写 {BrandPatched}、类型新增 {TypeAdded}、类型档案补写 {TypePatched}",
                    brandAdded, brandPatched, typeAdded, typePatched);
            }

            await ReconcileMerchantFollowerCountsAsync(context, logger);

            // 改动说明（2026-11-20 拼音检索）：商户与品牌的三列上线前创建的行检索键为空，此处幂等补齐
            await BackfillSearchKeysAsync(context, logger);
        }

        /// <summary>
        /// 回填拼音检索键：只处理全拼列为空的存量行（商户与品牌同一口径），逐条重算后保存。
        /// </summary>
        private static async Task BackfillSearchKeysAsync(ApplicationDbContext context, ILogger logger)
        {
            var merchants = await context.Merchants
                .Where(m => m.SearchPinyin == null)
                .ToListAsync();

            var brands = await context.Brands
                .Where(b => b.SearchPinyin == null)
                .ToListAsync();

            if (merchants.Count == 0 && brands.Count == 0)
            {
                return;
            }

            foreach (var merchant in merchants)
            {
                merchant.RefreshSearchKeys();
            }

            foreach (var brand in brands)
            {
                brand.RefreshSearchKeys();
            }

            await context.SaveChangesAsync();
            logger.LogInformation(
                "拼音检索键已回填：商户 {MerchantRows} 行、品牌 {BrandRows} 行",
                merchants.Count, brands.Count);
        }

        /// <summary>
        /// 校准商家粉丝数：按关注连接表重算，只在数值不一致时更新，避免每次启动都写库。
        /// 改动说明：连接表是事实源，Merchants.FollowerCount 只是读模型缓存；历史数据可能因早期未维护而漂移。
        /// </summary>
        private static async Task ReconcileMerchantFollowerCountsAsync(ApplicationDbContext context, ILogger logger)
        {
            var updated = await context.Database.ExecuteSqlRawAsync(
                """
                UPDATE "Merchants" AS m
                SET "FollowerCount" = c.cnt,
                    "UpdatedAt" = (now() at time zone 'utc')
                FROM (
                    SELECT "MerchantId", COUNT(*)::int AS cnt
                    FROM "UserMerchantFollows"
                    WHERE "IsActive" = true
                    GROUP BY "MerchantId"
                ) AS c
                WHERE m."Id" = c."MerchantId"
                  AND m."FollowerCount" <> c.cnt;

                UPDATE "Merchants" AS m
                SET "FollowerCount" = 0,
                    "UpdatedAt" = (now() at time zone 'utc')
                WHERE m."FollowerCount" <> 0
                  AND NOT EXISTS (
                      SELECT 1 FROM "UserMerchantFollows" AS f
                      WHERE f."MerchantId" = m."Id" AND f."IsActive" = true
                  );
                """);

            if (updated > 0)
            {
                logger.LogInformation("商家粉丝数已按关注表校准，影响行数 {Rows}", updated);
            }
        }
    }
}