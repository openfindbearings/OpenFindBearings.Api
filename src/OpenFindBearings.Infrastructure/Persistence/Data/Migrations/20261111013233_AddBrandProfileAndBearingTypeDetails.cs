using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandProfileAndBearingTypeDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Advantages",
                table: "Brands",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Brands",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FoundedYear",
                table: "Brands",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MainApplications",
                table: "Brands",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentCompany",
                table: "Brands",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuffixNotes",
                table: "Brands",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Brands",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnglishName",
                table: "BearingTypes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "BearingTypes",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSeparable",
                table: "BearingTypes",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoadCharacteristics",
                table: "BearingTypes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MainApplications",
                table: "BearingTypes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StructureFeatures",
                table: "BearingTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TypicalPrefixes",
                table: "BearingTypes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // 改动说明（品牌与类型资料完善）：为常见品牌与四大类型回填人工维护资料。
            // 仅在对应字段为空时写入，不覆盖后台已录入内容；回填行标记为人工来源，防同步任务覆盖。
            migrationBuilder.Sql(@"
UPDATE ""Brands"" SET ""Description"" = '全球最大的轴承制造商，滚动轴承技术标杆，产品覆盖标准轴承到轴承单元与润滑系统。', ""FoundedYear"" = 1907, ""Country"" = '瑞典', ""ParentCompany"" = 'SKF 集团', ""Website"" = 'https://www.skf.com', ""Advantages"" = '高速精密轴承、轴承单元、密封与润滑系统', ""SuffixNotes"" = '2RS1/2RSH=接触式密封；ZZ=铁盖防尘；ECP=玻纤增强尼龙保持架；C3=加大游隙', ""MainApplications"" = '风电、铁路、机床、汽车、冶金', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'SKF' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '日本首家轴承制造商，精密轴承与汽车轴承见长。', ""FoundedYear"" = 1916, ""Country"" = '日本', ""ParentCompany"" = 'NSK 株式会社', ""Website"" = 'https://www.nsk.com', ""Advantages"" = '汽车轮毂单元、精密机床轴承、直线导轨', ""SuffixNotes"" = 'DDU=接触密封；VV=非接触密封；CM=电机专用游隙；T85=高温', ""MainApplications"" = '汽车、电机、机床、家电', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'NSK' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '舍弗勒旗下品牌，高端工业与汽车轴承代表。', ""FoundedYear"" = 1883, ""Country"" = '德国', ""ParentCompany"" = '舍弗勒集团', ""Website"" = 'https://www.schaeffler.cn', ""Advantages"" = '主轴轴承、轧机轴承、轮毂轴承、关节轴承', ""SuffixNotes"" = '2RSR=接触密封；B=改进内部结构；TVH=尼龙保持架；XL=X-life 长寿命', ""MainApplications"" = '机床、冶金、风电、汽车', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'FAG' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '日本三大轴承品牌之一，等速万向节与轮毂单元强势。', ""FoundedYear"" = 1918, ""Country"" = '日本', ""ParentCompany"" = 'NTN 株式会社', ""Website"" = 'https://www.ntn.com', ""Advantages"" = '轮毂单元、等速万向节、精密轴承', ""SuffixNotes"" = 'LLU/LLB=密封；C3=加大游隙；G=特殊润滑', ""MainApplications"" = '汽车、工程机械、家电', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'NTN' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '圆锥滚子轴承发明者，重载领域标杆。', ""FoundedYear"" = 1899, ""Country"" = '美国', ""ParentCompany"" = '铁姆肯公司', ""Website"" = 'https://www.timken.com', ""Advantages"" = '圆锥滚子轴承、调心滚子轴承、特种钢材', ""SuffixNotes"" = 'C=改进内部设计；NA=英制系列；前缀按尺寸系列区分', ""MainApplications"" = '汽车、矿山、轧机、齿轮箱', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'TIMKEN' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = 'JTEKT 旗下品牌，与丰田体系配套紧密。', ""FoundedYear"" = 1921, ""Country"" = '日本', ""ParentCompany"" = 'JTEKT 株式会社', ""Website"" = 'https://www.jtekt.co.jp', ""Advantages"" = '汽车轴承、转向系统轴承、水泵轴承', ""SuffixNotes"" = '2RD/2RU=密封；C3=加大游隙', ""MainApplications"" = '汽车、农机、电机', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'KOYO' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '不二越旗下，液压与轴承并重。', ""FoundedYear"" = 1928, ""Country"" = '日本', ""ParentCompany"" = '那智不二越', ""Website"" = 'https://www.nachi-fujikoshi.co.jp', ""Advantages"" = '液压泵轴承、机器人关节轴承', ""SuffixNotes"" = '2NSE=接触密封；C3=加大游隙', ""MainApplications"" = '机器人、液压、汽车', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'NACHI' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '舍弗勒旗下，滚针轴承与直线导轨专家。', ""FoundedYear"" = 1946, ""Country"" = '德国', ""ParentCompany"" = '舍弗勒集团', ""Website"" = 'https://www.schaeffler.cn', ""Advantages"" = '滚针轴承、直线导轨、单向轴承', ""SuffixNotes"" = '2Z=双面防尘；P5/P4=精度等级', ""MainApplications"" = '机床、汽车、液压', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'INA' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '滚针轴承专业厂。', ""FoundedYear"" = 1950, ""Country"" = '日本', ""ParentCompany"" = 'IKO 株式会社', ""Website"" = 'https://www.ikont.co.jp', ""Advantages"" = '滚针轴承、直线导轨、凸轮从动件', ""SuffixNotes"" = 'P=精密级；T=特殊规格', ""MainApplications"" = '机床、包装机械、机器人', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'IKO' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '直线运动系统领导者。', ""FoundedYear"" = 1971, ""Country"" = '日本', ""ParentCompany"" = 'THK 株式会社', ""Website"" = 'https://www.thk.com', ""Advantages"" = '直线导轨、滚珠丝杠、直线轴承', ""SuffixNotes"" = 'LM=直线轴承系列代号', ""MainApplications"" = '机床、半导体设备、自动化', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'THK' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '国产老牌哈轴，通用轴承与精密轴承兼有。', ""FoundedYear"" = 1950, ""Country"" = '中国', ""ParentCompany"" = '哈尔滨轴承集团', ""Website"" = 'https://www.hrb.com.cn', ""Advantages"" = '深沟球轴承、圆柱滚子轴承、角接触球轴承', ""SuffixNotes"" = '2RZ=非接触密封；2RS=接触密封；P5/P4=精度', ""MainApplications"" = '电机、机床、农机', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'HRB' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '国产瓦轴，重载与大型轴承见长。', ""FoundedYear"" = 1938, ""Country"" = '中国', ""ParentCompany"" = '瓦房店轴承集团', ""Website"" = 'https://www.zwz-bearing.com', ""Advantages"" = '调心滚子轴承、轧机轴承、风电主轴轴承', ""SuffixNotes"" = 'CA/W33=球面滚子加外圈油槽；P5=精度', ""MainApplications"" = '冶金、风电、矿山', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'ZWZ' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '国产洛轴，大型与精密转盘轴承强势。', ""FoundedYear"" = 1954, ""Country"" = '中国', ""ParentCompany"" = '洛阳轴承集团', ""Website"" = 'https://www.lyc.cn', ""Advantages"" = '转盘轴承、精密轴承、航空航天轴承', ""SuffixNotes"" = '2Z=防尘；P4=精密', ""MainApplications"" = '工程机械、风电、航空', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'LYC' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '国内规模领先的通用轴承制造集团。', ""FoundedYear"" = 1991, ""Country"" = '中国', ""ParentCompany"" = '人本集团', ""Website"" = 'https://www.cugroup.com', ""Advantages"" = '深沟球轴承、汽车轮毂轴承、电机轴承', ""SuffixNotes"" = '2RS=接触密封；ZZ=铁盖防尘', ""MainApplications"" = '电机、家电、汽车', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'C&U' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '国产通用轴承品牌。', ""FoundedYear"" = 2004, ""Country"" = '中国', ""ParentCompany"" = '天马轴承集团', ""Website"" = 'https://www.tmbgroup.cn', ""Advantages"" = '深沟球轴承、调心滚子轴承', ""SuffixNotes"" = '沿用通用型号后缀', ""MainApplications"" = '电机、农机、通用机械', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'TMB' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '汽车轴承专业厂。', ""FoundedYear"" = 2000, ""Country"" = '中国', ""ParentCompany"" = '襄阳汽车轴承股份', ""Website"" = 'https://www.zxy.com.cn', ""Advantages"" = '轮毂轴承、离合器分离轴承', ""SuffixNotes"" = '沿用通用型号后缀', ""MainApplications"" = '商用车、乘用车', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'XCC' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '法国品牌，后并入 NTN 集团。', ""FoundedYear"" = 1916, ""Country"" = '法国', ""ParentCompany"" = 'NTN 集团', ""Website"" = 'https://www.ntn-snr.com', ""Advantages"" = '汽车轴承、轮毂单元', ""SuffixNotes"" = 'EE=密封；C3=加大游隙', ""MainApplications"" = '汽车、工业', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'SNR' AND ""Description"" IS NULL;
UPDATE ""Brands"" SET ""Description"" = '国产轴承套圈与成品轴承供应商。', ""FoundedYear"" = 2003, ""Country"" = '中国', ""ParentCompany"" = '五洲新春集团', ""Website"" = 'https://www.wzxc.cn', ""Advantages"" = '深沟球轴承、圆锥滚子轴承', ""SuffixNotes"" = '沿用通用型号后缀', ""MainApplications"" = '电机、汽车、农机', ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'WBJF' AND ""Description"" IS NULL;
UPDATE ""BearingTypes"" SET ""EnglishName"" = 'Deep Groove Ball Bearing', ""StructureFeatures"" = '内外圈各一道深沟槽，钢球与沟槽四点接触，结构简单、摩擦小', ""LoadCharacteristics"" = '以径向载荷为主，可承受一定双向轴向载荷', ""TypicalPrefixes"" = '60/62/63/64/68/69', ""MainApplications"" = '电机、家电、汽车、通用机械', ""IsSeparable"" = false, ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'DGBB' AND ""EnglishName"" IS NULL;
UPDATE ""BearingTypes"" SET ""EnglishName"" = 'Angular Contact Ball Bearing', ""StructureFeatures"" = '内外圈沟槽存在接触角，单列通常需成对安装', ""LoadCharacteristics"" = '可同时承受径向与单向轴向载荷，接触角越大轴向承载越高', ""TypicalPrefixes"" = '70/72/73/79/32/33', ""MainApplications"" = '机床主轴、减速机、泵', ""IsSeparable"" = false, ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'ACBB' AND ""EnglishName"" IS NULL;
-- 改动说明（2026-11-12 语义修正）：SRB 在平台数据中为调心球轴承，调心滚子轴承的编码是 SRLB，此处原写成调心滚子属编码语义错配
UPDATE ""BearingTypes"" SET ""EnglishName"" = 'Self-Aligning Ball Bearing', ""StructureFeatures"" = '外圈球面滚道配双列钢球，允许内外圈相对偏转，自动补偿同轴度误差', ""LoadCharacteristics"" = '以径向载荷为主，可承受少量轴向载荷，抗挠曲能力强', ""TypicalPrefixes"" = '12/13/22/23', ""MainApplications"" = '纺织机械、农机、输送设备、木工机械', ""IsSeparable"" = false, ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'SRB' AND ""EnglishName"" IS NULL;
UPDATE ""BearingTypes"" SET ""EnglishName"" = 'Tapered Roller Bearing', ""StructureFeatures"" = '圆锥滚子与锥形滚道线接触，通常成对安装以消除游隙', ""LoadCharacteristics"" = '承受径向与单向轴向联合载荷，刚性高', ""TypicalPrefixes"" = '302/303/313/320/322/323/330', ""MainApplications"" = '汽车、齿轮箱、轧机、工程机械', ""IsSeparable"" = true, ""DataSourceType"" = 'Manual', ""ImportedBy"" = 'seed-brand-profile', ""ImportedAt"" = now() WHERE ""Code"" = 'TRB' AND ""EnglishName"" IS NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Advantages",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "FoundedYear",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "MainApplications",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "ParentCompany",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "SuffixNotes",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "EnglishName",
                table: "BearingTypes");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "BearingTypes");

            migrationBuilder.DropColumn(
                name: "IsSeparable",
                table: "BearingTypes");

            migrationBuilder.DropColumn(
                name: "LoadCharacteristics",
                table: "BearingTypes");

            migrationBuilder.DropColumn(
                name: "MainApplications",
                table: "BearingTypes");

            migrationBuilder.DropColumn(
                name: "StructureFeatures",
                table: "BearingTypes");

            migrationBuilder.DropColumn(
                name: "TypicalPrefixes",
                table: "BearingTypes");
        }
    }
}
