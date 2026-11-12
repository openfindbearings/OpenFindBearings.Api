using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <summary>
    /// 品牌与类型基础档案回填（第二批，纯数据迁移，不改表结构）。
    /// 背景：迁移 20261111013233 按 18 个品牌编码、4 个类型编码回填，但线上库品牌仅 14 个、
    /// 类型表当时为空（类型数据后由同步管线导入），故仅 5 个品牌命中、类型 0 命中。
    /// 本迁移按库内真实编码补齐 9 个已有品牌的档案，并按主流市场补录 15 个基础品牌，
    /// 同时为 16 个轴承类型写全英文名、结构特征、承载特性、典型前缀、主要应用与可分离性。
    /// 一律判空写入（品牌按 Description、类型按 EnglishName），不覆盖后台人工录入内容；
    /// 新增品牌以 Code 唯一索引做 ON CONFLICT DO NOTHING，重复执行与爬取补录均安全。
    /// 手写迁移必须显式标注 [DbContext]，否则 EF 运行时静默忽略（见迁移 20261111093000 的说明）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261112120000_BackfillBrandAndTypeProfiles")]
    public partial class BackfillBrandAndTypeProfiles : Migration
    {
        /// <summary>
        /// 按真实编码回填品牌与类型的基础档案数据
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 改动说明（品牌档案补齐）：以下 9 个编码为线上库实际存在的品牌，此前未命中回填
            migrationBuilder.Sql(@"
UPDATE ""Brands"" SET ""Description"" = '重庆机电控股集团控股的国有股份制轴承企业，以汽车与摩托车轴承为主，主打低噪音密封深沟球轴承与轮毂轴承单元。', ""FoundedYear"" = 1990, ""Country"" = '中国', ""ParentCompany"" = '重庆机电控股（集团）公司', ""Website"" = 'https://www.cjb.com.cn', ""Advantages"" = '低噪音密封深沟球轴承、汽车轮毂单元、四点接触球轴承、圆柱滚子轴承、精密锻件', ""MainApplications"" = '汽车变速器与底盘、摩托车、机床、工程机械、电机', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'CJB' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '由镇江轴承厂改制而来的滚针轴承专业厂，国家“七五”“八五”数控机床国产化滚针轴承主要研制单位，江苏省高新技术企业。', ""FoundedYear"" = 1970, ""Country"" = '中国', ""Website"" = 'http://www.fyb-bearing.cn', ""Advantages"" = '滚针轴承、圆柱滚子轴承、推力轴承、角接触球轴承、复合轴承', ""MainApplications"" = '汽车、冶金、机床、纺织、印刷、轻工、工程机械', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'FYB' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '浙江慈溪民营轴承集团，1973 年创立，深沟球轴承出口规模居国内前列，近年重点切入机器人谐波减速器配套轴承。', ""FoundedYear"" = 1973, ""Country"" = '中国', ""ParentCompany"" = '环驰轴承集团（HCH Bearing Group）', ""Website"" = 'https://www.hchbearing.com', ""Advantages"" = '深沟球轴承、柔性薄壁轴承、交叉滚子轴承、四点接触球轴承、微型轴承', ""SuffixNotes"" = 'ZZ=铁盖防尘；2RS=接触密封', ""MainApplications"" = '家电、电机、汽车、机器人、工业自动化', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'HCH' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '山东临清推力轴承制造企业，主产推力球轴承、推力滚子轴承与调心滚子轴承，年产能三百万套以上。', ""Country"" = '中国', ""ParentCompany"" = '山东华钢精密轴承制造有限公司', ""Website"" = 'http://www.hogozc.com', ""Advantages"" = '推力球轴承、推力圆柱滚子轴承、推力圆锥滚子轴承、推力调心滚子轴承、调心滚子轴承', ""SuffixNotes"" = 'M=黄铜实体保持架；W33=外圈油槽与油孔', ""MainApplications"" = '齿轮装置、汽车、机床、注塑机、起重运输、矿山工程机械', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'HOGO' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '美国轴承品牌，1941 年起步，2008 年被 SKF 收购后作为独立品牌运营，聚焦农机、电机与物料搬运等目标行业。', ""FoundedYear"" = 1941, ""Country"" = '美国', ""ParentCompany"" = 'SKF 集团', ""Website"" = 'https://www.peerbearing.com', ""Advantages"" = '深沟球轴承、带座轴承单元、农机轴承、圆锥滚子轴承', ""MainApplications"" = '农业机械、电机、流体设备、工业传动、物料搬运、非公路车辆', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'PEER' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '优必胜（上海）精密轴承，2000 年注册 UBC 商标，2017 年成为日本 IKO 集团全资子公司，苏州设厂并沿用日系品质管控。', ""FoundedYear"" = 2000, ""Country"" = '中国', ""ParentCompany"" = '日本 IKO 集团（Nippon Thompson）', ""Website"" = 'https://www.ubc-bearing.com', ""Advantages"" = '圆柱滚子轴承、圆锥滚子轴承、调心滚子轴承、带座轴承、直线导轨与滚珠丝杠', ""MainApplications"" = '压缩机、风电、机器人 RV 减速器、电梯、工业齿轮箱、农机、矿山冶金', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'UBC' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '东莞怡合达自动化（深交所创业板 301029），FA 工厂自动化零部件一站式供应平台，轴承为其传动零部件品类之一。', ""FoundedYear"" = 2010, ""Country"" = '中国', ""ParentCompany"" = '东莞怡合达自动化股份有限公司', ""Website"" = 'https://www.yhdfa.com', ""Advantages"" = 'FA 零部件一站式供应、标准化选型与 3D 模型、短交期', ""MainApplications"" = '自动化设备、锂电与光伏装备、3C 制造、机床', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'YHD' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '1968 年建厂的国有汽车轴承基地，1996 年深交所上市（000678），现由湖北三环集团控股，2013 年收购波兰 KFLT。', ""FoundedYear"" = 1968, ""Country"" = '中国', ""ParentCompany"" = '湖北三环集团有限公司', ""Website"" = 'https://www.zxy.com.cn', ""Advantages"" = '汽车轮毂轴承单元、圆锥滚子轴承、圆柱滚子轴承、滚针轴承、等速万向节、离合器分离轴承', ""MainApplications"" = '商用车与乘用车整车配套、农机、风电、工程机械', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'ZXY' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '前身洛阳轴承研究所（1958 年成立），国机集团下属高新技术企业，专攻“高、精、尖、特、专”轴承，航空航天与机床为主战场。', ""FoundedYear"" = 1958, ""Country"" = '中国', ""ParentCompany"" = '中国机械工业集团有限公司（国机集团）', ""Website"" = 'https://www.zys.com.cn', ""Advantages"" = '精密与特种轴承、电主轴、轴承专用装备与检测仪器', ""MainApplications"" = '航空航天、舰船兵器、机床工具、风电、矿山冶金、石油化工、医疗器械、轨道交通', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'ZYS' AND ""Description"" IS NULL;
");

            // 改动说明（国际品牌补录）：库里仅 14 个爬取品牌，主流国际品牌缺口大，按市场常见度补录基础档案
            migrationBuilder.Sql(@"
INSERT INTO ""Brands"" (""Id"", ""Code"", ""Name"", ""Country"", ""Description"", ""FoundedYear"", ""ParentCompany"", ""Website"", ""Advantages"", ""MainApplications"", ""Level"", ""DataSourceType"", ""ImportedBy"", ""ImportedAt"", ""CreatedAt"", ""UpdatedAt"", ""IsActive"") VALUES
(gen_random_uuid(), 'NMB', '美蓓亚三美', '日本', '日本美蓓亚三美（MinebeaMitsumi）旗下轴承品牌，微型与小型精密轴承全球领先，电机与汽车电子配套为主。', 1951, 'MinebeaMitsumi 集团', 'https://www.nmbtc.com', '微型深沟球轴承、电机轴承、精密小型轴承', '微特电机、汽车电子、家电、办公设备', 2, 'Manual', 'seed-brand-profile', now(), now(), now(), true),
(gen_random_uuid(), 'FYH', 'FYH', '日本', '日本带座外球面轴承单元专业厂，UC/UK 系列外球面轴承与铸铁座体成套供应，农机与输送线常用。', 1950, 'FYH 株式会社', 'https://www.fyh.co.jp', '带座外球面轴承单元、外球面轴承芯', '输送机械、农机、纺织、食品机械', 2, 'Manual', 'seed-brand-profile', now(), now(), now(), true),
(gen_random_uuid(), 'ASAHI', '旭精工', '日本', '日本旭精工（ASAHI SEIKO）带座轴承品牌，外球面轴承单元系列齐全，注重密封与免维护。', 1956, 'ASAHI SEIKO 株式会社', NULL, '带座外球面轴承单元、不锈钢轴承单元', '输送线、食品与包装机械、农机', 2, 'Manual', 'seed-brand-profile', now(), now(), now(), true),
(gen_random_uuid(), 'EZO', 'EZO', '日本', '日本微型轴承品牌，以不锈钢与深沟球微型轴承见长，常用于小电机与精密仪器。', 1950, 'EZO 株式会社', NULL, '微型不锈钢轴承、薄壁轴承', '微特电机、仪器仪表、办公设备、模型', 2, 'Manual', 'seed-brand-profile', now(), now(), now(), true),
(gen_random_uuid(), 'KBC', 'KBC', '韩国', '韩国通用轴承品牌，规格覆盖深沟球、圆锥滚子与带座单元，性价比路线。', 1969, 'KBC 株式会社', NULL, '深沟球轴承、圆锥滚子轴承、带座轴承单元', '电机、汽车、农机、通用机械', 2, 'Manual', 'seed-brand-profile', now(), now(), now(), true),
(gen_random_uuid(), 'ZKL', 'ZKL', '捷克', '捷克老牌轴承集团，欧洲本土制造，通用轴承与铁路、冶金配套经验丰富。', 1926, 'ZKL Group', 'https://www.zkl.cz', '深沟球轴承、调心滚子轴承、圆锥滚子轴承、铁路轴承', '铁路、冶金、矿山、通用机械', 2, 'Manual', 'seed-brand-profile', now(), now(), now(), true),
(gen_random_uuid(), 'FKL', 'FKL', '塞尔维亚', '塞尔维亚轴承制造商，主产通用工业轴承与农机轴承，欧洲二线性价比品牌。', 1963, 'FKL 集团', NULL, '深沟球轴承、圆锥滚子轴承、农机轴承、带座单元', '农机、矿山、输送、通用机械', 5, 'Manual', 'seed-brand-profile', now(), now(), now(), true),
(gen_random_uuid(), 'URB', 'URB', '罗马尼亚', '罗马尼亚大型轴承集团，重载与大型轴承见长，广泛用于轧机、矿山与能源装备。', 1953, 'URB 集团', NULL, '调心滚子轴承、圆柱滚子轴承、圆锥滚子轴承、大型轴承', '冶金轧机、矿山、水泥、能源装备', 5, 'Manual', 'seed-brand-profile', now(), now(), now(), true),
(gen_random_uuid(), 'NKE', 'NKE', '奥地利', '奥地利工业轴承品牌，专注标准工业轴承与定制方案，欧洲中端定位。', 1996, 'NKE Austria GmbH', 'https://www.nke.at', '深沟球轴承、调心滚子轴承、圆柱滚子轴承、轴承座', '风电、冶金、输送、通用机械', 2, 'Manual', 'seed-brand-profile', now(), now(), now(), true),
(gen_random_uuid(), 'RHP', 'RHP', '英国', '英国老牌轴承品牌，现属 NSK 集团，精密与通用轴承并重，欧洲市场认知度高。', 1899, 'NSK 集团', 'https://www.nsk.com', '深沟球轴承、角接触球轴承、带座单元', '电机、机床、纺织、通用机械', 2, 'Manual', 'seed-brand-profile', now(), now(), now(), true),
(gen_random_uuid(), 'Barden', 'Barden', '美国', '美国精密轴承品牌，现属舍弗勒集团，以高精度角接触球轴承服务机床主轴与航空。', 1907, '舍弗勒集团', 'https://www.schaeffler.com', '精密角接触球轴承、主轴轴承', '机床主轴、航空、医疗、真空泵', 1, 'Manual', 'seed-brand-profile', now(), now(), now(), true),
(gen_random_uuid(), 'GMN', 'GMN', '德国', '德国家族轴承企业，精密主轴轴承与高速电主轴并重，机床与涡轮领域口碑稳定。', 1908, 'GMN Paul Müller Industrie GmbH', 'https://www.gmn.de', '精密角接触球轴承、主轴单元、单向离合器', '机床主轴、涡轮机械、真空泵', 1, 'Manual', 'seed-brand-profile', now(), now(), now(), true)
ON CONFLICT (""Code"") DO NOTHING;
");

            // 改动说明（国内品牌补录）：补国内有明确品牌编码的常见厂商，编码不确定者留给爬取补录
            migrationBuilder.Sql(@"
INSERT INTO ""Brands"" (""Id"", ""Code"", ""Name"", ""Country"", ""Description"", ""FoundedYear"", ""ParentCompany"", ""Website"", ""Advantages"", ""MainApplications"", ""Level"", ""DataSourceType"", ""ImportedBy"", ""ImportedAt"", ""CreatedAt"", ""UpdatedAt"", ""IsActive"") VALUES
(gen_random_uuid(), 'CXD', '慈兴', '中国', '宁波慈兴集团，微小型深沟球轴承与汽车轴承规模领先，主攻电机与汽车配套市场。', 1984, '慈兴集团有限公司', 'https://www.cxbearing.com', '微小型深沟球轴承、汽车轴承、电机轴承', '微特电机、汽车、家电、电动工具', 3, 'Manual', 'seed-brand-profile', now(), now(), now(), true),
(gen_random_uuid(), 'QC', '万向钱潮', '中国', '万向集团旗下汽车零部件上市公司，轴承业务覆盖轮毂单元、圆锥滚子与传动轴配套。', 1994, '万向集团', 'https://www.wxqc.com.cn', '汽车轮毂轴承单元、圆锥滚子轴承、传动轴', '汽车整车配套、底盘与传动系统', 3, 'Manual', 'seed-brand-profile', now(), now(), now(), true),
(gen_random_uuid(), 'NF', '南方轴承', '中国', '常州南方轴承，滚针轴承与单向离合器专业厂，汽车与摩托车配套为主。', 1988, '江苏南方精工股份有限公司', NULL, '滚针轴承、单向离合器、汽车张紧轮轴承', '汽车变速器、摩托车、农机、电动工具', 4, 'Manual', 'seed-brand-profile', now(), now(), now(), true)
ON CONFLICT (""Code"") DO NOTHING;
");

            // 改动说明（类型档案补齐）：线上库 16 个类型此前全部为空档案，按库内中文名逐一对齐写入
            // 注意 SRB 在库内为调心球轴承、SRLB 为调心滚子轴承，编码语义不可混用
            migrationBuilder.Sql(@"
UPDATE ""BearingTypes"" SET ""EnglishName"" = 'Deep Groove Ball Bearing', ""StructureFeatures"" = '内外圈各一道深沟槽，钢球与沟槽四点接触，结构简单、摩擦小、易维护', ""LoadCharacteristics"" = '以径向载荷为主，可承受一定双向轴向载荷', ""TypicalPrefixes"" = '60/62/63/64/68/69', ""MainApplications"" = '电机、家电、汽车、通用机械、精密仪器', ""IsSeparable"" = false, ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'DGBB' AND ""EnglishName"" IS NULL;
UPDATE ""BearingTypes"" SET ""EnglishName"" = 'Angular Contact Ball Bearing', ""StructureFeatures"" = '内外圈沟槽存在接触角，单列通常需成对安装（面对面、背对背或串联）', ""LoadCharacteristics"" = '径向与单向轴向联合承载，接触角越大轴向能力越强', ""TypicalPrefixes"" = '70/72/73/74/79/32/33', ""MainApplications"" = '机床主轴、减速机、泵、空压机、高频电机', ""IsSeparable"" = false, ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'ACBB' AND ""EnglishName"" IS NULL;
UPDATE ""BearingTypes"" SET ""EnglishName"" = 'Self-Aligning Ball Bearing', ""StructureFeatures"" = '外圈球面滚道配双列钢球，允许内外圈相对偏转，自动补偿同轴度误差', ""LoadCharacteristics"" = '以径向载荷为主，可承受少量轴向载荷，抗挠曲能力强', ""TypicalPrefixes"" = '12/13/22/23', ""MainApplications"" = '纺织机械、农机、输送设备、木工机械', ""IsSeparable"" = false, ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'SRB' AND ""EnglishName"" IS NULL;
UPDATE ""BearingTypes"" SET ""EnglishName"" = 'Spherical Roller Bearing', ""StructureFeatures"" = '外圈球面滚道配双列鼓形滚子，允许内外圈相对偏转，承载能力高于调心球轴承', ""LoadCharacteristics"" = '径向载荷为主，可承受双向轴向载荷，抗冲击与过载能力强', ""TypicalPrefixes"" = '213/222/223/230/231/232/239/240/241', ""MainApplications"" = '冶金轧机、矿山、风机、造纸、水泥', ""IsSeparable"" = false, ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'SRLB' AND ""EnglishName"" IS NULL;
UPDATE ""BearingTypes"" SET ""EnglishName"" = 'Cylindrical Roller Bearing', ""StructureFeatures"" = '滚子与滚道线接触，套圈带挡边，多数型号内外圈可分离，便于装拆', ""LoadCharacteristics"" = '承受较大径向载荷，刚性好、转速高，一般不承受轴向载荷', ""TypicalPrefixes"" = 'N/NU/NJ/NUP/NN/NNU + 2/3/4 尺寸系列', ""MainApplications"" = '电机、轧机、齿轮箱、机床主轴、压缩机', ""IsSeparable"" = true, ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'CRB' AND ""EnglishName"" IS NULL;
UPDATE ""BearingTypes"" SET ""EnglishName"" = 'Tapered Roller Bearing', ""StructureFeatures"" = '锥形滚子与锥形滚道线接触，各锥面汇交于轴心线同一点，多成对安装使用', ""LoadCharacteristics"" = '承受径向与单向轴向联合载荷，刚性高，单列需配对消除游隙', ""TypicalPrefixes"" = '302/303/313/320/322/323/330/331/332', ""MainApplications"" = '汽车车桥与变速器、齿轮箱、轧机、工程机械', ""IsSeparable"" = true, ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'TRB' AND ""EnglishName"" IS NULL;
UPDATE ""BearingTypes"" SET ""EnglishName"" = 'Needle Roller Bearing', ""StructureFeatures"" = '细长滚针作滚动体，径向截面小，分带内圈、无内圈与冲压外圈等形式', ""LoadCharacteristics"" = '径向承载能力大而占用径向空间小，不承受轴向载荷', ""TypicalPrefixes"" = 'HK/BK/NA/NK/RNA/NAV/NKI', ""MainApplications"" = '汽车变速器、万向节、液压泵、纺织机械', ""IsSeparable"" = true, ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'NRB' AND ""EnglishName"" IS NULL;
UPDATE ""BearingTypes"" SET ""EnglishName"" = 'Thrust Ball Bearing', ""StructureFeatures"" = '轴圈、座圈与钢球组合，单向型承受单向轴向力，双向型两套并列布置', ""LoadCharacteristics"" = '仅承受轴向载荷，不能承受径向载荷', ""TypicalPrefixes"" = '511/512/513/514/522/523/532/533/542/543', ""MainApplications"" = '起重机吊钩、立式泵、机床、减速机', ""IsSeparable"" = true, ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'TBB' AND ""EnglishName"" IS NULL;
UPDATE ""BearingTypes"" SET ""EnglishName"" = 'Thrust Roller Bearing', ""StructureFeatures"" = '滚子轴线与轴心线垂直布置，含推力圆柱、推力圆锥与推力调心滚子三类', ""LoadCharacteristics"" = '承受很大的单向或双向轴向载荷，刚性高，不能承受径向载荷', ""TypicalPrefixes"" = '811/812/813/893/894/292/293/294', ""MainApplications"" = '轧机压下机构、船舶推进、挤出机、立式电机', ""IsSeparable"" = true, ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'THRB' AND ""EnglishName"" IS NULL;
UPDATE ""BearingTypes"" SET ""EnglishName"" = 'Linear Ball Bearing', ""StructureFeatures"" = '外筒内装钢球与保持架，沿光轴往复直线运动，分标准型、法兰型与开口型', ""LoadCharacteristics"" = '承受径向载荷，摩擦小、运动平稳，不适合承受旋转载荷', ""TypicalPrefixes"" = 'LM/LME/LMB/LMK/LMH/LMF/KH', ""MainApplications"" = '3D 打印机、自动化模组、包装机械、测量设备', ""IsSeparable"" = false, ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'LB' AND ""EnglishName"" IS NULL;
UPDATE ""BearingTypes"" SET ""EnglishName"" = 'Spherical Plain Bearing', ""StructureFeatures"" = '内圈外球面与外圈内球面滑动接触，可承受多方向偏转与摆动', ""LoadCharacteristics"" = '承受径向与任意方向轴向联合载荷，适合摆动与低速转动', ""TypicalPrefixes"" = 'GE/GEG/GEE/SI/SA/PHS', ""MainApplications"" = '工程机械油缸、液压支架、转向拉杆、机器人关节', ""IsSeparable"" = true, ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'SPB' AND ""EnglishName"" IS NULL;
UPDATE ""BearingTypes"" SET ""EnglishName"" = 'Slewing Bearing', ""StructureFeatures"" = '大直径内外圈带齿或安装孔，集成滚道与密封，单套实现回转支撑', ""LoadCharacteristics"" = '同时承受轴向、径向与倾覆力矩，以低速重载工况为主', ""TypicalPrefixes"" = '01/02/11/13/22 系列', ""MainApplications"" = '挖掘机、起重机、风电变桨、雷达与光伏跟踪', ""IsSeparable"" = true, ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'SWB' AND ""EnglishName"" IS NULL;
UPDATE ""BearingTypes"" SET ""EnglishName"" = 'Mounted Bearing Unit', ""StructureFeatures"" = '外球面轴承装入铸铁或冲压座体，座体带安装孔，可自动调心', ""LoadCharacteristics"" = '以径向载荷为主，可承受中等轴向载荷，密封性好适合粉尘环境', ""TypicalPrefixes"" = 'UCP/UCF/UCFL/UCPH/UCPA/UCFC/UCT', ""MainApplications"" = '输送机、农机、纺织印染、矿山、环保设备', ""IsSeparable"" = false, ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'HUB' AND ""EnglishName"" IS NULL;
UPDATE ""BearingTypes"" SET ""EnglishName"" = 'Insert Ball Bearing', ""StructureFeatures"" = '深沟球轴承变型，外圈外径为球面，内圈加长并带顶丝或偏心套', ""LoadCharacteristics"" = '以径向载荷为主，可承受径向与轴向联合载荷，不宜单独承受轴向力', ""TypicalPrefixes"" = 'UC/UE/SA/SB/UK/UD', ""MainApplications"" = '带座单元配套、农机、输送线、食品机械', ""IsSeparable"" = false, ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'IB' AND ""EnglishName"" IS NULL;
UPDATE ""BearingTypes"" SET ""EnglishName"" = 'Combination Bearing', ""StructureFeatures"" = '径向轴承与推力轴承集成为一套，共用内圈或外圈，结构紧凑', ""LoadCharacteristics"" = '同时承受径向与轴向载荷，安装与对中要求较高', ""TypicalPrefixes"" = 'NKX/NKXR/RAX/ZARF/ZARN', ""MainApplications"" = '丝杠支撑、减速机、泵、机床进给系统', ""IsSeparable"" = true, ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'CMB' AND ""EnglishName"" IS NULL;
UPDATE ""BearingTypes"" SET ""EnglishName"" = 'Track Roller Bearing', ""StructureFeatures"" = '外圈为加厚滚动面，内圈或螺栓轴带安装结构，含凸轮从动件与支承滚轮', ""LoadCharacteristics"" = '承受径向载荷与一定冲击，外圈直接作为滚道在导轨上滚动', ""TypicalPrefixes"" = 'NUTR/NUKR/NATR/NAST/STO/CF', ""MainApplications"" = '凸轮机构、输送导轨、自动化滑台、包装机械', ""IsSeparable"" = false, ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'TKB' AND ""EnglishName"" IS NULL;
");
        }
    }
}
