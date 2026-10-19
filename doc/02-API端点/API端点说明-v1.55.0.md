# API 端点说明文档

**版本：** v1.55.0
**日期：** 2026-10-19
**上一版：** v1.54.0
**状态：** 与代码同步

---

## 变更记录
| v1.39.0 | 2026-09-26 | 权限目录重排端点侧改键（端点数量不变 167）：品牌端点组 bearing.*→brand.view/create/edit/delete、类型端点组→type.*、points rules→points.manage、按-auth 角色读/assign/移除与 provision→user.assign（Admin 专属防提权）、批量角色映射→user.view；键目录全表见 04-用户角色权限设计 v1.7.0 |

| 版本 | 日期 | 变更说明 |
|------|------|----------|
| v1.55.0 | 2026-10-19 | **邀标边界补全与契约纠正（公开招标/邀标 v1.54.0 的完整化）**：① **应答守卫**——`POST /api/sourcing/demands/{id}/respond` 对邀标单只允许收件商家应答（按发布时点快照判定，非收件商家报错，堵住"知 id 即插队应答"）；② **详情可见性放宽**——邀标单对"发布商户的在职成员"（原只认经办人本人）与"任一收件商户的在职成员"（原只认当前选中商户，一人多商户深链会误 404）可见；③ **通知免限频**——邀标单豁免 `Sourcing.NotifyMaxPerMerchantPerDay`（受邀是点名勾选、受众可控，"受邀必达"优先；公开招标保持限频）；④ **契约纠正**：发布响应 `notifiedMerchantCount` 更名 `recipientMerchantCount`（原名义暗示"实际投递家数"，实际还取决于收件商户是否有在职管理员）；`visibility` 做枚举范围校验、公开招标携带 `invitedMerchantIds` 直接 400（不再静默忽略）；匹配/额度顺序调整（"无人可邀"在 NEED_POINTS 确认框之前暴露）。⑤ **口径统一**：`SourcingMatcher` 复用实体 `IsPubliclyVisible` 与商户 `Status=Active`，商户名义发布排除发布商户自己；新增 `IMerchantBearingRepository.GetMatchableByMerchantAsync`（审核通过 且 出售中/补货中），`/opportunities`、`/matched` 与匹配口径一致（原只在售口径导致补货中商家被通知却在聚合面看不到）；`/matched` 邀标单优先参与截断（原按时间倒序取 50，较早邀标单可能被挤出）。⑥ **管理端点**：`GET /api/admin/sourcing/demands` 与详情补 `publisherMerchantId/publisherMerchantName/publisherType`。⑦ 撤回应答同步递减 `ResponseCount`（原撤标后计数残留，`/matched`、`/opportunities` 的"尚无人应答"判定失准）。端点数量不变。明细见文末"本版增量"章节 |
| v1.54.0 | 2026-10-19 | **寻货公开招标 / 邀标两轴模型**：`SourcingDemand` 弃用 `TargetMerchantId`，改 `Visibility`（0公开招标/1邀标）+ `InvitedMerchantIds`（邀标收件人快照 `uuid[]`，空=全部匹配的认证在售商家）；邀标单不入公开大厅、仅发布人与收件商家可见（非收件商家取详情 404）。`POST /api/sourcing/demands` 请求体以 `visibility`+`invitedMerchantIds` 取代 `targetMerchantId`，响应 `matchedMerchantCount` 更名 `notifiedMerchantCount`（公开=匹配家数、邀标=勾选∩匹配家数）；`/demands`（大厅）仅列公开单；`/demands/{id}`、`/my/demands`、`/merchant/demands`、`/matched`、`/opportunities` 透出 `isPrivate`；新增 `POST /api/sourcing/demands/{id:guid}/publish-public`（发布人把进行中的邀标单放开为公开）。`SourcingDemandPublishedEvent` 改为携带**最终收件人集合**（订阅者不再自行匹配）。迁移 `AddSourcingVisibility`（加 `Visibility`/`InvitedMerchantIds` + gin 索引，存量 `TargetMerchantId` 非空行回填 `Visibility=1`、收件人=[该商户] 后删旧列）。寻货端点组 10→11（本版净 +1）。明细见文末"本版增量"章节 |
| v1.53.0 | 2026-10-18 | **替代关系独立权限键**：替代品（替代关系）从借用 `merchant.verify`（认证审核语义）改为独立键 `interchange.view`（查看替代关系）与 `interchange.manage`（维护替代关系），回归平台主数据（与轴承/品牌/型号同维）的权限口径。端点权限：`GET /api/admin/interchanges` → `interchange.view`；`POST/PUT/DELETE /api/admin/interchanges[/{id:guid}]` → `interchange.manage`。权限目录 32→34 键；迁移 `AddInterchangePermissions`（幂等补插 2 键 + 给 Admin/Operator/Auditor 授权，角色计数 Operator 18→20、Auditor 11→12）。端点数量不变，明细见文末"本版增量"章节 |
| v1.52.0 | 2026-10-18 | **替代品字典化：撤销"商家候选 + 平台审核"**——替代关系回归**平台维护的主数据（字典）**：爬取候选仍在 Sync staging 审核（进 API 即生效），平台在管理页人工维护/按纠错核实。① 移除 5 个端点：`GET /api/admin/interchanges/pending`、`POST /api/admin/interchanges/{id:guid}/approve`、`POST /api/admin/interchanges/{id:guid}/reject`、`POST /api/merchant/interchanges`、`GET /api/merchant/interchanges`；② `GET /api/admin/interchanges` 去掉 `reviewStatus` 参数；③ `GET /api/bearings/{id:guid}/interchanges` 语义改为"全部生效关系"（审核态过滤随状态机删除，行为等价）；④ 仪表盘 `interchanges.pendingCount` 删除（恒为 0）；⑤ 纠错新增可纠错字段 `interchange`（替代关系报告；审批不自动应用，由平台在替代品管理页落地）。迁移 `RemoveInterchangeReview`（删 `ReviewStatus`/`ReviewComment`/`ProposedByMerchantId` 三列与索引，先清除历史未生效候选行）。后台管理端点组 75→72、商家端点组 26→24。明细见文末"本版增量"章节 |
| v1.51.0 | 2026-10-18 | **寻货发布回传匹配家数（搜索—寻货两条路径可见化）**：`POST /api/sourcing/demands` 响应由 `{ id }` 扩为 `{ id, matchedMerchantCount }`——广播单=本次**已匹配并推送的认证商家家数**（口径=公域可见在售本轴承 + 已认证），定向单=1，无匹配=0；前端据此区分"已通知 N 家认证商家"与"仅进入寻货大厅等应答"。匹配逻辑抽出 `SourcingMatcher.GetMatchedMerchantIdsAsync`，由**发布处理器**（回传家数）与**发布通知订阅者**（逐户推送）共用，消除两处口径漂移（含无 `bearingId` 时按型号文本精确匹配主数据兜底）。端点数量不变。配套《搜索寻货与数据可信联动设计 v1.1.0》。明细见文末"本版增量"章节 |
| v1.50.0 | 2026-10-17 | **仪表盘"新增"改真值并按来源拆分 + 商家入驻时间字段**（消除假数据）：① 新增 `Merchant.OnboardedAt`（入驻生效时间，`Approve()` 幂等写入；迁移 `AddMerchantOnboardedAt` 加列 + 回填存量已入驻商户为 `CreatedAt`）；② `GET /api/admin/dashboard/stats` 重排：轴承、替代品新增按 **互联网 / 人工** 拆分，商家新增按 **互联网同步 / 入驻（`OnboardedAt`，含认领）** 拆分（三者均含今日/本周/本月），新增顶层 `periods`（业务日历起点，供图表下钻）与 `interchanges` 统计块、`merchants.onboardedCount`（已入驻真值）；移除旧字段 `merchants.todayRegistered/thisWeekRegistered/thisMonthRegistered` 与 `bearings.todayAdded/thisWeekAdded/thisMonthAdded`；③ `GET /api/admin/merchants` 新增参数 `onboardedFrom`/`onboardedTo`（入驻时间区间，业务日历含端点）、`internetSource`（true=仅互联网 / false=仅非互联网，来源拆分行下钻用），`sortBy` 增 `onboardedat`；`MerchantDto` 增 `onboardedAt`。④ **下钻筛选贯通**：`GET /api/admin/bearings` 新增 `createdFrom`/`createdTo`/`internetSource`（`BearingDto` 增 `createdAt`）；`GET /api/admin/interchanges` 新增 `createdFrom`/`createdTo`/`internetSource`。端点数量不变（仅字段与参数增量）。明细见文末"本版增量"章节 |
| v1.49.0 | 2026-10-15 | **搜索—寻货—认证商家与数据可信联动**（配套《搜索寻货与数据可信联动设计 v1.0.0》）：① 通知偏好：新增 `GET/PUT /api/notifications/preferences`（渠道开关 + `smsGatewayReady`；写复用 UpdateUserPreferenceCommand）。② 寻货联动：`POST /api/sourcing/demands` 请求体加可选 `targetMerchantId`（定向询价，仅推目标商户、不进大厅；迁移 `AddSourcingTargetMerchant`）；新增 `GET /api/sourcing/matched`（商家侧"为你匹配的寻货"：定向给我的 + 匹配我在售型号的进行中广播）；发布新增领域事件 `SourcingDemandPublishedEvent` → 匹配"在售本轴承的认证商家"多渠道通知（站内信 + 外部通道按偏好，`Sourcing.NotifyMaxPerMerchantPerDay` 默认 3 限频）；`GET /api/sourcing/demands/{id}` 输出加 `isTargeted`。③ 数据可信标注：`BearingDto` 加 `DataSource`、`BearingMerchantDto` 加 `IsVerified`（询价行动门"仅认证商家可询价"依据）。④ 电话单轨：公共查询 `MerchantDto` 移除 `Phone/Mobile/Email`，`MerchantDetailDto` 改嵌套 `Contact`（仅本商户成员自看下发）。⑤ 深链修复：寻货通知 `bizType` 字面量 `"SourcingDemand"` → 常量 `Notification.BizSourcingDemand`（`sourcing_demand`）。⑥ 僵尸清理：删 `GetMerchantByUserIdQuery(Handler)`。端点总数 187 → 189。明细见文末"本版增量"章节 |
| v1.48.0 | 2026-10-13 | **商家商品审核出口 + 状态统一 + 替代品三级治理端点补记**：① 补齐"有骨架无出口"的商品审核：Admin 组新增 `GET /api/admin/merchant-bearings/pending`、`POST /api/admin/merchant-bearings/{id:guid}/approve`、`POST /api/admin/merchant-bearings/{id:guid}/reject`（merchant.verify，Admin「商品审核」页数据源）。② 商品状态契约变更：`MerchantBearingDto` 的 `isOnSale/isRestocking/isPendingApproval/isFeatured` 四字段收敛为 `saleState`（0已下架/1出售中/2补货中）+ `reviewStatus`（0仓库中/1审核中/2已通过/3已驳回，`isFeatured` 为僵尸字段已删）；新增 `DELETE /api/merchant/bearings/{id:guid}`（出售中/补货中禁删，其余物理删除）；"上架"语义改为未通过→提交审核、已通过→直接上架；公域三面统一过滤 `reviewStatus=2 且 saleState∈{1,2}`。③ 替代品三级治理：Admin 组新增 `GET/POST/PUT/DELETE /api/admin/interchanges[/{id:guid}]` + `GET /api/admin/interchanges/pending` + `POST /api/admin/interchanges/{id:guid}/approve|reject`（merchant.verify；该组权限已于 v1.53.0 独立为 interchange.view/interchange.manage）；商家组新增 `POST /api/merchant/interchanges`、`GET /api/merchant/interchanges`；`GET /api/bearings/{id:guid}/interchanges` 改只放已生效、返回含 `typeLabel` 富 DTO（三级：标准等效/条件替代(需评估)/仅供参考）。商家端点组 23→26、后台管理端点组 65→75，端点总数 174→187。明细见文末"本版增量"章节 |
| v1.47.0 | 2026-10-10 | **等级玩法两批次端点补记（承《11-积分体系设计 v2.12.0/v2.13.0》《等级玩法设计 v1.1.0/v1.1.1》，补上批未登记欠账）**：v2.12.0 侧新增 `GET/PUT /api/admin/points/levels`（段位档位表读写：阈值/段位名/升档礼/启停，points.manage 权限，Admin"等级段位"tab 数据源）；`GET /api/points/account` 响应追加 `nextLevelMin/nextLevelName/nextLevelBonus`、`POST /api/points/checkin` 追加 `level/levelName/leveledUp`、`GET /api/points/merchant-buff` 追加 `gradeUpBonus/graceUntil`（均纯加字段）；寻货发布人视图应答行下发经办人 `responderLevel/responderLevelName`。v2.13.0 侧新增 `GET /api/points/levels`（段位阶梯表：全档位+本人落档+升档礼已领标记，已领按 `levelup:{userId}:{档}` 流水存在性判定）与 `GET /api/points/merchant-grade?merchantId=`（商家等级详情：双指标进度/四档阶梯/已领档/保级钟，**在职成员校验**）；迁移 RenamePremiumMerchantGradeLabel：商家 Lv3 "活跃供给"改名"口碑商家"（四档统一"XX商家"格式，展示名硬编码在 Merchant.GetGradeDisplayName，迁移只同步 SystemConfigs 描述文案）。端点总数 170 → 174 |
| v1.46.0 | 2026-10-07 | **开源化：Sync/Crawler 集成面整体摘除**：删除同步端点组 /api/sync（6 个，SyncEndpoints + Commands/Sync + Sync DTO）、商户库存导入判权端点 GET /api/merchant/inventory/import-context、内部配置端点 GET /api/config/reliability（外部 ETL 运行时拉取面）、商户释放事件总线（close/detach 唤醒通知随之外移，端点仅落库不再发外部事件）与 SyncClient 授权策略；端点总数 170 → 163；SeedData 权限目录移除 merchant.import/mapping.view/mapping.manage/review.sync/sync.run；Admin/BFF 同步移除对应管理面板与代理编排。**同版本追加（商户名义发布）**：发布端点 `POST /api/sourcing/demands` 请求体加可选 `merchantId`（在职成员校验前置、个人额度沿用）；feed/my-demands/detail 三处输出 `publisherMerchantId/publisherMerchantName/publisherType`；选定解锁的发布方联系方式分流——商户单给商户公开电话（Contact.Mobile ?? Phone），个人单维持注册手机；迁移 AddDemandPublisherMerchant（SourcingDemands 两列+索引）；**商户寻货工作台**：新增 `GET /api/sourcing/merchant/demands`（X-Merchant-Id 在职成员校验，商户名义全状态单+经办人昵称+isMine），`GET /api/sourcing/my/demands` 改为只返回个人名义单（双体系拆分）；寻货端点 10 → 11，端点总数 163 → 164。**同版本追加（全站列表统一删除）**：SourcingDemands 加 IsDeleted 软删列（迁移 AddDemandSoftDelete，feed/我的/商户三列表过滤）+ `POST /api/sourcing/demands/batch-delete`（终态+经办人守卫，进行中拒删计 skipped）；通知 `POST /batch-read`、`POST /batch-delete`；`POST /api/me/favorites/bearings/batch-remove`、`/follows/merchants/batch-remove`、`/history/batch-delete`——六个批量端点，端点总数 164 → 170 |
| v1.45.0 | 2026-10-01 | 寻货应答升级**多行标书**（对齐《12-寻货功能设计 v1.5.0》《BFF v1.13.0》《Taro v1.7.30》）：新增 `SourcingResponseItems` 行表（迁移 `AddSourcingResponseItems`=建表 + 旧单型号应答按需求型号回填为一行 + 删主表 Price/Stock/LeadTime 列，顺序不可反）；`POST /api/sourcing/demands/{id}/respond` 请求体改 `items[]`（每行 partNumber 必填 + bearingId 可引在售 + price/stock/leadTime 可选，至少一行），重复应答=整份替换行；detail 发布人视图/Admin 详情/`GET /my/responses` 装配均透出 `items`；detail 发布人每条应答增透 **实力摘要** `companyName/onSaleCount/completedTaskCount`（发布人选定的结构化依据）；**同版本追加**：新增 `DELETE /api/sourcing/demands/{id}/respond` 撤销应答（仅待处理可撤、行表级联删除、当日额度不退还）与仓储 `RemoveAsync`；端点 10 个 |
| v1.44.0 | 2026-09-30 | 寻货 feed 筛选与信息架构收敛（对齐《12-寻货功能设计 v1.4.0》《BFF v1.12.0》《Taro v1.7.29》）：`GET /api/sourcing/demands` 删除 `mineOnly` 参数（"我的寻货"改走既有 `GET /my/demands` 独立端点，发现页回归纯大厅），新增 `brand`/`region`（包含匹配，与 keyword AND 正交）与 `sort`（asc\|desc 发布时间升降序，置顶恒排前不受排序影响）；仓储 `GetListAsync` 同步（删 mineOnlyUserId、增 brand/region/newestFirst）；端点总数不变 |
| v1.43.0 | 2026-09-29 | **架构调整：API 对 Sync 零出站**（对齐《06 v2.20.0》《BFF v1.10.0》《Sync v17.6.0》《部署架构 v1.3.0》）：① 关店/解除归属唤醒 Sync staging 由"事务内同步 HTTP 调 Sync"改为**发布 Redis Stream 事件**（`merchant:release-events`，新 `MerchantReleaseEventBus`；Redis 未启用降级日志告警，Sync 消费者组至少一次拉取），闭店端点行为不变；② `POST /api/merchant/inventory/import`（API 中转 Sync）**下线**，改 `GET /api/merchant/inventory/import-context`（判权+返回权威 merchantId），文件流由 BFF 直传 Sync，端点总数不变；③ 删除 `SyncInventoryService`/`SyncStagingRefreshService` 与 appsettings `Sync` 配置节；④ 安全轮换：`Internal:ApiToken`（限流豁免令牌）真实值从 appsettings 移除改 REPLACE_ME，生产经 infra Secret 注入（旧值曾随公开库泄露已废弃，中间件空值=永不豁免 fail-closed）；⑤ Redis 基建接入（CacheSettings:EnableRedis + CacheSettings:RedisConnectionString 由 K8s Secret 注入） |
| v1.40.0 | 2026-09-27 | M1-a/M1-b/M1-c 增量补录：成就 5 端点、商城目录/兑换/订单 5 端点、商家金库与挂礼 10 端点、礼品兑换/确认收货 2 端点、Admin 挂礼审核与托管订单 5 端点；端点总数 167→189。明细见文末"本版增量"章节 |
| v1.38.0 | 2026-09-26 | 角色显示名分离：Role 实体/RoleDto 新增 DisplayName（可空，Keycloak 式标识/显示分离）；CreateRole 的 Name 限英文标识（^[A-Za-z][A-Za-z0-9_]*$）、中文名走 DisplayName；迁移 AddRoleDisplayName（官方工具生成含 Designer）为内置四角色赋中文名（管理员/操作员/审计员/App用户）；GET /api/admin/users/platform-roles 批量端点（sub→roles[] 字典，Admin 用户列表角色列合并防 N+1）；修复 GetRoleDetailQuery 基类强转派生类 InvalidCastException 隐性 500；POST /api/admin/users/provision 预置业务用户（按 sub find-or-create + 批量挂角色，解后台新建账号未登录过不能分配角色的 404 死结）；端点总数 166→167 |
| v1.37.0 | 2026-09-26 | 备案拆分与客服电话接线：MobileConfigDto 新增 BeiAnApp/BeiAnMini 字段，GetMobileConfigQueryHandler 读 Mobile.BeiAnApp/Mobile.BeiAnMini 两键；SeedData 补两键（EnsureConfigKeys 幂等补全，无需迁移）并明确 Site.BeiAn=网站备案语义 |
| v1.0.0 | 2026-06-12 | 初始版本 |
| v1.1.0 | 2026-06-21 | 补充 Admin 端点权限表（17→46 端点） |
| v1.2.0 | 2026-07-06 | 全量对齐当前代码，覆盖 110 个端点，修正路由不一致、遗漏、过时项 |
| v1.3.0 | 2026-07-07 | 整合 Doc/Admin/Permission 命名统一；新增 POST /api/merchants/batch-check-verified（Sync 调用，批量查询商户认证状态） |
| v1.4.0 | 2026-07-07 | 新增 POST /api/merchant/apply（预留）、POST /api/sms/send-code（Identity）、POST /api/admin/merchants/{id}/reject；POST /api/admin/merchants/{id}/verify 新增清除爬虫数据逻辑；批量认证查询端点移至 /api/sync/merchants/batch-check-verified |
| v1.5.0 | 2026-07-07 | 新增 GET /api/admin/merchants/{id}（按 ID 查询商家详情，供 Sync 库存导入解析）|
| v1.6.0 | 2026-07-07 | 修复：BatchCheckVerifiedQueryHandler N+1（改为单 SQL 批量查询）；ApproveLicenseCommandHandler 补充爬虫数据清除和 verifiedBy；UpdateMerchantBearing/SetPriceVisibility/PutOnShelf/TakeOffShelf 四个端点新增商家所有权验证；GetMyMerchantBearings 的 pendingOnly 下推到仓储数据库层过滤 |
| v1.7.0 | 2026-07-07 | 删除虚构的 ETL 触发（6.9）和爬虫管理（6.10）端点章节（这些端点属于 Sync/Crawler 项目，不在 API 中） |
| v1.8.0 | 2026-07-09 | 移除 POST /api/sync/merchants/batch-check-verified 端点（不再需要已认证商户预检）；移除 VerifyMerchantCommandHandler/ApproveLicenseCommandHandler 中 DeleteByMerchantAndSourceAsync 调用（审核不再清除爬虫数据）；移除 IMerchantBearingRepository.DeleteByMerchantAndSourceAsync 和 IMerchantRepository.GetVerifiedNamesAsync；GET /api/merchants/{id}/bearings 新增 dataSource（Manual/Crawler）和 onlyOnSale（bool）查询参数；端点总数 114 → 113，同步端点 7 → 6 |
| v1.9.0 | 2026-08-20 | GET /api/admin/merchants 新增 excludeCrawler（bool）查询参数，排除爬虫来源商家，仅显示入驻申请商家；SearchMerchantsQuery/MerchantSearchParams 同步支持 ExcludeCrawler |
| v1.10.0 | 2026-08-29 | 新增 GET /api/bearings/{id:guid}/merchants（轴承在售商家反向查询，支持 onlyOnSale 与分页参数），端点总数 113 → 114，公共端点组 12 → 13 |
| v1.11.0 | 2026-08-29 | 新增 GET /api/config/reliability（可信度阈值，供 Sync 运行时从 SystemConfigs 拉取，替代其 appsettings 默认值）；系统配置章节补充该内部端点 |
| v1.12.0 | 2026-09-08 | GET /api/bearings/search 移除"至少一个搜索条件"强校验（原抛 InvalidOperationException 被映射为 400），无条件时返回空/全量分页，与商家搜索一致；限流中间件新增内部服务间调用豁免（X-Internal-Token，见《系统配置与限流设计 v1.1.0》） |
| v1.13.0 | 2026-09-11 | 代码审查对齐：① 公共端点组补充 GET /api/login-methods（13→14）；② 用户端点组补充 POST /api/me/avatar、GET /api/me/corrections、GET /api/me/corrections/{id}（21→24）；③ 管理端点 6.2 轴承管理补充 DELETE /api/admin/bearings/{id:guid}/hard；④ 总端点数 114→118 |
| v1.14.0 | 2026-09-12 | 商户入驻整体落地：① 新增"商户入驻"端点组（6 个）：POST /api/merchant/apply（预留转正式实现）、GET /api/merchant/application、POST /api/merchant/nominate、POST /api/merchant/nominate/{code}/accept、GET /api/merchant/claimable、GET /api/merchant/nominations/pending；② 商家端点组 12→16：新增 POST /api/merchant/members/{userId:guid}/suspend、POST /api/merchant/members/{userId:guid}/activate、PUT /api/merchant/members/{userId:guid}/role、POST /api/merchant/inventory/import（Excel 批量导入，仅商户管理员），并补成员管理小节；③ Admin 商家管理 58→60：新增 POST /api/admin/merchants/{id}/approve（审核通过入驻申请，Pending→Active，与 verify 认证分离）、POST /api/admin/merchants/{id}/members（平台指定成员兜底，复用 merchant.verify）；④ 总端点数 118→129，引导表格分组同步更新 |
| v1.15.0 | 2026-09-14 | 商户信息维护与 Logo + 新建撞名引导认领：① 商家端点组 16→17，新增 POST /api/merchant/logo（上传商户 Logo，仅商户管理员，返回相对 URL，落 wwwroot/uploads/merchants/logo，.DisableAntiforgery）；② GET /api/merchant/profile 改按 X-Merchant-Id 当前商户上下文定位（原按"首个在职成员"，多商户读写错位），PUT /api/merchant/profile 补 IsMerchantAdmin 校验（原仅成员即可）；③ POST /api/merchant/apply（mode=self）新增撞名查重：命中可认领商户返回 HTTP 409 + ProblemDetails code=MERCHANT_CLAIMABLE_EXISTS/existingMerchantId/existingName（新增 MerchantClaimableConflictException + 中间件映射），命中已认证/已认领返回 400；④ MerchantApplicationDto 与 MerchantDetailDto 新增字段（LogoUrl；Detail 补 Website/UnifiedSocialCreditCode），MerchantExtensions.ToDetailDto 补齐 LogoUrl 等映射。总端点数 129→130 |
| v1.16.0 | 2026-09-14 | 申请人自助撤回入驻申请：① 入驻端点组 6→7，新增 `POST /api/merchant/{merchantId:guid}/withdraw`（仅 Pending 且调用者是该商户在职 MerchantAdmin 即申请人本人可撤回；按 `Merchant.ApplicationMode` 分支清理——self 新建硬删商户及成员、claim 认领软移除认领人成员并把来源退回 Crawler 重新进入认领池、nomination/None 拒绝）。② Merchant 新增 `ApplicationMode` 列（None/Self/Claim/Nomination，默认 None 兼容存量），迁移 `AddMerchantApplicationMode`。总端点数 130→131 |
| v1.18.0 | 2026-09-15 | 版本分发机制改为集群拉取：Taro CI 删除跨境 `kubectl cp` 推包与 psql 写库步骤（上行 ~50KB/s 阻塞发布且需集群凭据），发布动作纯 GitHub 化；K3s 侧新增 CronJob `openfindbearings-apk-sync`（Taro 仓库 `deploy/apk-server/apk-server.yml`，每晚 4 点北京时间）从 GitHub Release 拉最新规范版本分包（资产 sha256 校验 + `wget -c` 断点续传），两包齐备才改写 `Mobile.AppVersion`/`Mobile.UpdateMessage`，拉不完不宣告；端点行为与 `Mobile.*` 键族语义不变 |
| v1.19.0 | 2026-09-16 | 媒体服务改独立 nginx 直出（应用不再逐字节代理图片）：① 新增 `deploy/k3s/media-server.yml`（nginx 只读挂 sync hostPath `/opt/openfindbearings/sync/images` + api hostPath `/opt/openfindbearings/api/uploads`，对外 `<your-bff-domain>/media/{images,uploads}/**`）+ API `deploy.yml` uploads hostPath 权限从 700 改 755（媒体服务以不同 uid 只读挂载需 other 位读穿越）；② `Mobile.*` 新增第六键 `MediaBaseUrl`（`https://<your-bff-domain>/media`），`GET /api/mobile/config` 响应新增 `mediaBaseUrl` 字段，前端 `getMediaBase()` 拼接；③ URL 契约治本：库内只存相对媒体键，`UserEndpoints.UploadAvatar`/`MerchantEndpoints.UploadLogo` 保持返回相对 `/uploads/...`（前端与 BFF 都不再拼绝对），`Identity/UpdateProfileRequest.PictureUrl` `[Url]` 改相对/绝对双允许正则（此前 `[Url]` 强制绝对，是历史绝对地址入库根因）；④ 幂等一次性 SQL 归一历史绝对 URL：`deploy/k3s/media-url-migration.sql`；⑤ BFF `MediaEndpoints.cs` + `ApiClient.GetRawAsync` + `MeEndpoints.PublicUrl` 删除（BFF 端点 -1） |
| v1.20.0 | 2026-09-16 | 审批主流化改造 + 站内信：① 新增"站内信"端点组（4 个，`/api/notifications`，Bearer Authenticated）：`GET /`（收件箱分页，unreadOnly 筛选）、`GET /unread-count`、`POST /{id:guid}/read`（越权 404）、`POST /read-all`；Notifications 表（写扩散 1 对 1），迁移 `AddNotifications`；事件驱动：`Merchant.Approve/Reject` 新增 `MerchantApprovedEvent/MerchantRejectedEvent` 领域事件，订阅者向商户在职管理员落站内信；接受提名发布 `NominationAcceptedEvent` 通知发起人（被提名人在途环节由邀请链接触达——API User 无手机号，未注册用户站内信不可达）；`INotificationService.AddInAppAsync` 落地（原 SendToUserAsync 为日志占位），订阅在业务事务提交后独立 SaveChanges，通知失败不回滚业务。② 审批并发守卫：`POST /api/admin/merchants/{id}/approve` 与 `/reject` 在商户非 Pending 时返回 **409** + `code=MERCHANT_ALREADY_PROCESSED`（原领域守卫抛 InvalidOperationException 被映射 400，语义不准）。③ 企业名称必填：`POST /api/merchant/apply`（self/claim）与 `POST /api/merchant/nominate/{code}/accept` 对 `companyName` 空值返回 400。④ `MerchantDto` 补 `applicationMode/submittedAt/rejectReason` 三字段并修复 `ToPublicDto` **漏映射 Status** 的根因缺陷（Admin 审批列表 Status 恒空导致"审核通过/拒绝"按钮不显示）。总端点数 131→135，组数 7→8 |
| v1.21.0 | 2026-09-16 | 被拒申请修改重提与删除（申请人在自助通道闭环）：① 入驻端点组 7→10，新增 `GET /api/merchant/{merchantId:guid}/application`（申请详情，重提预填，非成员 404）、`POST .../resubmit`（Suspended→Pending 修改重提，self/claim 限渠道，companyName/name 必填、查重排除自身）、`POST .../delete-application`（删除被拒申请，Self 硬删/Claim 退回认领池，与撤回共用 `ApplicantApplicationCleanup` 清理逻辑）。② `Merchant` 新增 `Resubmit()`（清驳回原因、恢复有效、回 Pending）与 `UpdateType()` 领域方法。修复 self 被拒后同名再提交被查重 400 永久卡死的死角（重提通道不产生新商户行）。总端点数 135→138 |
| v1.22.0 | 2026-09-19 | **入驻材料分层（LicenseVerification→MerchantDocument 泛化）**：① 新增 `DocumentType`（1 营业执照/2 品牌授权书/3 厂房照片）；apply/resubmit/accept 请求体 `licenseUrl` 废弃改 `documents[]`（类型矩阵校验：全类型必执照、AuthorizedDealer 另必授权书、商家类型升必填），审批通过/拒绝随单材料级联 Approved/Rejected，`verify` 改按"必备材料已全部通过"口径校验。② `POST /api/merchant/license`→`POST /api/merchant/documents`（带 type，即时建待审记录）；新增 `GET /api/merchant/documents`（当前商户材料列表）、`POST /api/merchant/documents/upload`（随单材料预上传只回 URL）；新增 `GET /api/admin/merchants/{id}/documents`（审批抽屉材料区）。③ `/api/admin/licenses/*` 三端点改名 `/api/admin/documents/*`（拒绝可带 reason）。迁移 `AddMerchantDocuments`（表/列/索引 rename + Type 回填 1，数据无损）。总端点数 138→141 |
| v1.23.0 | 2026-09-20 | **上传统一 MinIO 对象存储 + 材料队列口径修复**：① 用户上传（头像/Logo/证照材料/预上传）落盘从直写 wwwroot 改走 `IObjectStorageService` 抽象（生产 MinIO bucket=media key=`uploads/**`，开发本地盘，`FileStorage:Provider` 切换；S3 协议 AWS SDK 客户端），库内相对键 `/uploads/...` 与端点契约不变；删除从未消费的 `IFileService/LocalFileService` 死代码。部署与迁移见 doc/09 指南。② 修复 `Merchant` 材料导航 backing field 命名（`_documents`→`_merchantDocuments`，不匹配 EF 约定导致随单材料落库抛 Collection is read-only、入驻申请提交整批回滚）。③ `GET /api/admin/documents/pending` 队列与仪表盘角标同口径：Include 商家/提交人（修复商家列空、提交人未知），排除入驻审核中（Pending）商户的随单材料（随单材料仅在审批抽屉审结，消除双入口重复审核）。总端点数不变 141 |
| v1.24.0 | 2026-09-20 | **申请认证闭环 + 成员 isSelf + Mobile 僵尸清理**：① 新增 `POST /api/merchant/{merchantId:guid}/verify-request`（入驻组 10→11）：商户管理员主动申请认证，按材料矩阵校验（与 Admin 认证同口径，不齐 400 透传缺项），`Merchant.VerifyRequested` 标记（迁移 `AddMerchantVerifyRequested`），Admin 列表/抽屉显"申请认证"徽标，`verify` 成功自动清除；`MerchantDto`/`MerchantApplicationDto` 透传 `verifyRequested`。② `GET /api/merchant/staff` 成员项加 `isSelf`（后端权威标记——Taro 登录态 id 是 Identity sub 与成员 UserId 不同源，前端自判恒 false 致本人行误露操作按钮）。③ 自家商品列表 DTO 补四项描述与 `isPendingApproval`（编辑回填）；BFF `CreateBearingRequest` 字段对齐修复添加商品断链（原 partNumber/price 与后端 BearingId/PriceDescription 完全对不上，提交必失败）。④ 僵尸清理：删除 `/api/mobile/bearings/light` 与 `/api/mobile/home`（Taro 首页走 BFF 聚合通用端点、light 全链零消费）及 GetMobileBearingLightList/GetMobileHome 查询、MobileHomeDto/MobileBearingLightDto；`MobileConfigDto` 删无消费方的 MinVersion/Endpoints/Settings。总端点数 141→140 |
| v1.25.0 | 2026-09-21 | **员工邀请确认制 + 字段锁定 + 全量发现搜索**：① 商家端点组 19→23，新增 `GET /api/merchant/staff/invitations/pending`（待我确认员工邀请，JWT phone/email claim 服务端匹配）、`POST /api/merchant/staff/invitations/{invitationId:guid}/accept|decline|revoke`（接受建成员行+通知发起人 / 拒绝 / 管理员撤销）；`POST /api/merchant/staff` 对已注册用户由静默拉入改为创建 `StaffInvitation(Type=Staff)` 待确认邀请+站内信（message 透传真实文案），`GET /api/merchant/staff` 合并 `Status=Invited` 邀请行（含 `invitationId`）。② `PUT /api/merchant/{id}`（UpdateMerchant）Active 后拒绝变更企业名称/统一社会信用代码（与执照绑定，同值/未传放行，400）。③ `GET /api/merchant/claimable` 升级为全量发现搜索：DTO 加 `isClaimable/isMine/statusText`（可认领/我的商户/已入驻/审核中/已认证），搜索阶段阻断重复新建。④ 依赖 Identity 新增服务间端点 `GET /api/users/by-phone|by-email`（原 404 是添加成员静默走邀请分支的根因）。总端点数 140→144 |
| v1.26.0 | 2026-09-21 | **信用代码必填 + 类型锁定 + 邀请去重**（对齐《06 v2.11.0》，端点数不变 144）：① `POST /api/merchant/apply` 信用代码必填（18 位执照字符集）；resubmit/accept-nomination 按合并后值校验（存量有效不要求重输）。② `PUT /api/merchant/{id}` 守卫细化：企业名称一律锁定；信用代码"空→一次性补录"放行（同口径格式校验）、非空改值拒绝；商家类型 Active 后锁定（并接通原注释掉的 UpdateType，非 Active 真正可改）。③ `POST /api/merchant/staff` 两分支按同商户+同联系方式去重 Pending 邀请（幂等返回既有 invitationId，修复重复"已邀请"行）。④ Taro 同步：apply 三路径必填校验、profile 类型只读+信用代码空可补。⑤ 同版本追加：`GET staff` 成员项加 `mobile`/`joinedAt`（成员详情面板，User.Mobile 缓存列经登录中间件 JIT 同步）；accept/decline 邀请服务端核销对应站内信（未读角标即时消减）且端点补传 Email claim 支持邮箱注册用户处理邀请。端点数不变 144。 |
| v1.27.0 | 2026-09-21 | **站内信删除能力**（对齐《06 v2.12.0》，站内信组 4→6，总数 144→146）：① `DELETE /api/notifications/{id:guid}` 硬删本人单条（左滑删除，非本人/不存在 404）；② `DELETE /api/notifications/read` 清空本人全部已读（未读保留防误删漏看，返回 `{ affected }`）。ExecuteDelete 单 SQL 直删。 |
| v1.36.1 | 2026-09-25 | **商品三态与寻货联动**（对齐《12-寻货功能设计 v1.2.0》）：① MerchantBearing 加 IsRestocking/RestockEta（迁移 AddMerchantBearingRestocking），三态=在售/补货中/已下架，新端点 POST /api/merchant/bearings/{id}/restock；爬虫 L 阶段自动上架加 !IsRestocking 守卫（人工补货声明不被爬虫覆盖）；② 轴承商家列表（GetMerchantsByBearing）契约改 BearingMerchantDto 行级结构（merchantId/merchantName/price/isOnSale/isRestocking/restockEta），在售筛选扩为在售+补货中；③ 寻货组 +2 端点：GET /api/sourcing/my-offering（应答预填数据源）、GET /api/sourcing/opportunities（需求信号）；寻货详情响应补 bearingId；④ 端点总数 166→170 |
| v1.35.0 | 2026-09-25 | **寻货全链路 + 积分任务端点**（对齐《12-寻货功能设计 v1.1.0》《11-积分体系设计 v1.4.0》）：① 新增寻货组 /api/sourcing 9 端点（feed/详情/发布/应答/选定/取消/我的发布/我的应答/额度聚合）+ Admin 寻货治理 3 端点（列表/详情/下架）；② 积分组 /api/points 4 端点（account/checkin/transactions/tasks，v1.34.0 已声明但正文缺节，本次补齐）+ Admin 规则 2 端点；③ 纠错采纳默认分值 10→20（迁移种子，存量库 Admin 可调）；④ 端点总数修正为 166（此前概述 148 为 v1.29 旧数未随批次更新） |
| v1.34.0 | 2026-09-24 | **积分底座上线**（对齐《11-积分体系设计 v1.0.0》）：新增积分组 3 端点 `GET /api/points/account`（余额/累计/今日签到态/连击数）、`POST /api/points/checkin`（阶梯签到）、`GET /api/points/transactions`（流水分页）；Admin 组新增 2 端点 `GET/PUT /api/admin/points/rules[/{id}]`（赚分规则配置，system.manage 权限，实时生效）；端点总数 151→156。赚端 5 场景（登录/签到/注册/纠错采纳/入驻通过）经 PointsService 唯一写入口接线，扣分入口就位场景留白。 |
| v1.31.0 | 2026-09-24 | 权限体系接线（对齐《04 v1.6.0》）：① 用户组 25→26 新增 `GET /api/me/permissions`（当前用户角色+权限清单，Admin 登录门禁/菜单/复核数据源）；② Admin 组新增 by-auth 三端点 `GET/POST /api/admin/users/by-auth/{sub}/roles`、`DELETE .../roles/{roleName}`（Admin 用户页以 Identity sub 为键分配平台角色）；③ Admin 组删除权限写端点 POST/PUT/DELETE /api/admin/permissions（权限点目录只读）；端点总数 150→151。 |
| v1.30.0 | 2026-09-23 | 关店与解除归属（对齐《06 v2.17.0》）：入驻端点组 7→8 新增 `POST /api/merchant/{merchantId:guid}/close`（任一在职管理员自助关店：claim/提名已有 release 回公海、self/提名新建删除；全员站内信）；Admin 端点组新增 `POST /api/admin/merchants/{id:guid}/detach`（merchant.detach 权限，强制解除归属回公海）；总数 148→150。注销增强（API 内部）：本人待审纠错硬删+发起提名作废+匿名化级联补纠错；既有 withdraw/删被拒/注销的商户删除路径共享修复纠错 TargetId Restrict FK（DeleteByTargetAsync）。 |
| v1.29.0 | 2026-09-22 | 纠错闭环补齐（对齐《06 v2.15.0》）：① 用户端点组 24→25，新增 GET /api/me/corrections/fields/{targetType}/{targetId}（可纠错字段清单，与审批 Apply 分支严格同集合）；② 提交端点加去重守卫（同用户同实体同字段 Pending 幂等 400）；③ Approve/Reject 发 CorrectionProcessedEvent——站内信通知提交人结果，积分奖励订阅者预留扩展点。端点 147→148。
| v1.28.0 | 2026-09-22 | 新增 POST /api/me/deactivate（账户注销真闭环，端点 146→147）：守卫式注销（生效商户唯一管理员拦截）+ 申请撤回/成员移除/邀请作废/通知清理 + Identity 禁用与全设备令牌吊销 + 中间件对已注销账户存量 token 401 + UserDeactivationJob 冷静期满匿名化（配套迁移 AddUserDeactivation）。 |
| v1.17.0 | 2026-09-14 | 移动端版本更新落地：① `GET /api/mobile/version/check` 版本比较升级为真 SemVer 2.0 语义（NuGet.Versioning，兼容 `v` 前缀与 `rc.N` prerelease；原 int.Parse 实现遇 prerelease 必抛恒判"无更新"，解析失败退化为字符串不等兜底）；② 系统配置新增 `Mobile.*` 五键（AppVersion/MinVersion/ForceUpdate/DownloadUrl/UpdateMessage，SeedData 播种 + EnsureConfigKeysAsync 幂等补全，无需迁移），发布新版时在 Admin 系统配置页更新 AppVersion/DownloadUrl 即对客户端生效；③ `Mobile.DownloadUrl` 语义为 APK 下载目录（以 `/` 结尾，K3s 自建静态服务），客户端按 `app-v<版本>-<ABI>.apk` 拼文件名；④ BFF（OpenFindBearings.Mobile）新增匿名代理 `GET /mobile/config`（修复原 404）与 `GET /mobile/version/check` |

---

## 1. 概述

OpenFindBearings.Api（以下简称 API）共注册 **163** 个端点，按职责划分为 7 组。

| 组 | 路由前缀 | 端点数量 | 认证策略 |
|---|---------|---------|---------|
| 公共 | `/api` | 13 | 全部匿名 |
| 移动端 | `/api/mobile` | 2 | 全部匿名 |
| 用户 | `/api/me` | 24 | Bearer（Authenticated 策略） |
| 站内信 | `/api/notifications` | 6 | Bearer（Authenticated 策略） |
| 入驻 | `/api/merchant` | 10 | Bearer（任意已登录用户） |
| 商家 | `/api/merchant` | 19 | Bearer（Merchant 策略，分组限流） |
| 后台管理 | `/api/admin` | 61 | Bearer（Admin 策略）+ RequirePermission |

---

## 2. 公共端点 `/api`（13 个）

组授权：`AllowAnonymous()`。

| 方法 | 路由 | 说明 | WithName |
|------|------|------|----------|
| GET | `/api/bearings/search` | 搜索轴承（无条件时返回空/全量分页，不再 400） | `SearchBearings` |
| GET | `/api/bearings/hot` | 热门轴承 | `GetHotBearings` |
| GET | `/api/bearings/{id:guid}` | 轴承详情 | `GetBearingById` |
| GET | `/api/bearings/by-code/{partNumber}` | 按型号查询轴承 | `GetBearingByPartNumber` |
| GET | `/api/bearings/{id:guid}/interchanges` | 轴承替代品列表 | `GetBearingInterchanges` |
| GET | `/api/bearings/{id:guid}/merchants` | 轴承在售商家（反向查询，支持 onlyOnSale 与分页） | `GetMerchantsByBearing` |
| GET | `/api/brands` | 品牌列表 | `GetAllBrands` |
| GET | `/api/bearing-types` | 轴承类型列表 | `GetAllBearingTypes` |
| GET | `/api/merchants/search` | 搜索商家 | `SearchMerchants` |
| GET | `/api/merchants/{id:guid}` | 商家详情 | `GetMerchantById` |
| GET | `/api/merchants/{id:guid}/bearings` | 商家在售轴承（支持 dataSource 和 onlyOnSale 参数） | `GetMerchantBearings` |
| POST | `/api/sms/send-code` | 发送短信验证码（Identity 项目提供） | `SendSmsCode` |
| GET | `/api/login-methods` | 登录方式信息（返回 token 端点、注销端点、注册端点、支持的 grant 类型） | `GetLoginMethods` |

> v1.14.0 说明：`POST /api/merchant/apply` 已由"预留"转为正式实现，归入第 4 章"商户入驻"端点组（需登录），不再列于公共组；公共组 14 → 13。

> `GET /api/merchants/{id}/bearings` 支持以下查询参数：
> - `dataSource`：可选值 `Manual`（默认）、`Crawler`。未传时按 Manual 优先展示
> - `onlyOnSale`：可选值 `true`、`false`（默认）。设为 `true` 时仅返回在售记录

> `GET /api/bearings/{id:guid}/merchants` 支持以下查询参数：
> - `onlyOnSale`：可选值 `true`、`false`（默认 `true`）。设为 `true` 时仅返回在售商家
> - `page` / `pageSize`：分页参数（默认 page=1、pageSize=20）

---

## 3. 移动端点 `/api/mobile`（2 个）

组授权：`AllowAnonymous()`

| 方法 | 路由 | 说明 | WithName |
|------|------|------|----------|
| GET | `/api/mobile/config` | 移动端配置（含站点名称/备案号/客服联系方式/媒体源 base） | `GetMobileConfig` |
| GET | `/api/mobile/version/check` | 版本检查（SemVer 比较，见下方 v1.17.0 说明） | `CheckVersion` |

> v1.17.0 说明：
> - `GET /api/mobile/version/check` 支持 `currentVersion` / `platform` 查询参数。版本号按 **SemVer 2.0** 比较（NuGet.Versioning）：先比核心号（`1.0.1-rc.1 > 1.0.0-rc.12`），核心相同再比 prerelease（`rc.12 > rc.1`），`v` 前缀自动剥离；解析失败退化为"字符串不等即提示更新"兜底。
> - 服务端版本配置读取 `Mobile.{platform}.Version`，回退 `Mobile.AppVersion`（同理 MinVersion / ForceUpdate / DownloadUrl / UpdateMessage 五键族）。`Mobile.DownloadUrl` 是 APK 下载**目录**（以 `/` 结尾，K3s 自建静态服务 `https://<your-bff-domain>/dl/`），客户端按 `app-v<版本>-<ABI>.apk` 拼文件名；GitHub Release 为拉取源（服务器 CronJob 同步），不直接面向用户分发。
> - 五键由 SeedData 播种，存量库经 `EnsureConfigKeysAsync` 启动时幂等补全（无 EF 迁移）。发布新版流程（v1.18.0 起为集群拉取式）：Publish Release（规范 tag `vX.Y.Z[-rc.N]`）→ Taro CI 构建 APK 传 Release（跨境推包/写库步骤已删除）→ K3s CronJob `openfindbearings-apk-sync` 每晚 4 点（北京时间）拉最新规范版本分包（sha256 校验、`-c` 断点续传 + shell 重试循环抗链路抖动），两包齐备才改写 `Mobile.AppVersion`、写固定模板 `Mobile.UpdateMessage`，并清理历史版分包只留当前版；拉不完不宣告、下轮续传。Admin 系统配置页仅作手动兜底。
> - v1.19.0 媒体：图片不再由 BFF 逐字节代理，改独立 nginx 媒体服务在 `<your-bff-domain>/media/**` 直出。`GET /api/mobile/config` 响应含 `mediaBaseUrl`（读 `Mobile.MediaBaseUrl`，如 `https://<your-bff-domain>/media`，末尾无斜杠）。**媒体 URL 契约：库内一律存相对键**（`/images/...` 爬虫、`/uploads/...` 用户上传、`/avatars/presets/...` 预置），host 由客户端 `getMediaBase()` 拼（可被 `mediaBaseUrl` 运行时覆盖）；上传端点（头像/商户 Logo/执照）返回相对键。换域名/切对象存储只需改 `Mobile.MediaBaseUrl`，库内键不变。预置头像打包进 Taro 客户端本地，不经服务器。

---

## 4. 入驻端点 `/api/merchant`（11 个）

组授权：`RequireAuthorization()`（任意已登录用户即可，非 Merchant 策略）。

| 方法 | 路由 | 说明 | WithName |
|------|------|------|----------|
| POST | `/api/merchant/apply` | 提交入驻申请（mode=self 自助新建 / mode=claim 认领爬虫商家；申请人成为该商户管理员）。**v1.15.0**：mode=self 撞名命中可认领商户返回 HTTP 409 + `code=MERCHANT_CLAIMABLE_EXISTS`（携 `existingMerchantId/existingName`，供前端引导改认领），命中已认证/已认领返回 400。**v1.20.0**：`companyName`（营业执照企业名称）必填，空值返回 400。**v1.22.0**：`type` 升必填；`licenseUrl` 废弃，改 `documents[]`（`{type,fileUrl}`，矩阵：全类型必 1 营业执照、`AuthorizedDealer` 另必 2 品牌授权书，生产厂家选传 3 厂房照），缺失返回 400 | `ApplyMerchant` |
| GET | `/api/merchant/application` | 查询当前用户在各商户的入驻进度（待审核/已生效/已拒绝）。**v1.15.0**：返回项新增 `logoUrl`（供移动端 TabBar/商户切换器显示当前商户头像） | `GetMerchantApplication` |
| POST | `/api/merchant/nominate` | 提名他人为管理员（模式 B，建 Draft 商户 + Nomination 邀请；发起人默认入伙为员工） | `NominateMerchant` |
| POST | `/api/merchant/nominate/{code}/accept` | 被提名人接受提名并补全资料（Draft → Pending；手机号取自 JWT 防冒领）。**v1.20.0**：`companyName` 必填（合并后仍为空返回 400）；**v1.22.0**：补资料须随单 `documents[]`（必含营业执照，被提名商户为授权经销商时另需品牌授权书）；接受成功经 `NominationAcceptedEvent` 向发起人发站内信 | `AcceptNomination` |
| GET | `/api/merchant/claimable` | **v1.25.0 全量发现搜索**：关键词匹配的非草稿商户（含已入驻/审核中/已认证），每项带 `isClaimable`（未认证+无在职成员+无提名锁定）/`isMine`（当前用户在职成员）/`statusText`（可认领/我的商户/已入驻/审核中/已认证），Taro 在搜索阶段即阻断重复新建；支持 keyword/page/pageSize | `GetClaimableMerchants` |
| GET | `/api/merchant/nominations/pending` | 按当前登录用户手机号匹配待我接受的管理员提名邀请 | `GetPendingNominations` |
| GET | `/api/merchant/{merchantId:guid}/application` | **v1.21.0** 查询单个入驻申请详情（被拒重提表单预填数据源）：返回全量可编辑资料 + status/rejectReason/applicationMode；调用者须为该商户在职成员，否则 404（不泄露存在性）。**v1.22.0**：返回补 `documents[]`（材料类型/URL/状态/驳回意见），重提页"缺什么补什么"回显 | `GetMerchantApplicationDetail` |
| POST | `/api/merchant/{merchantId:guid}/resubmit` | **v1.21.0** 被拒后修改资料重新提交（Suspended→Pending 重走审核）：仅 `Suspended` 且调用者是在职 MerchantAdmin；渠道限 self/claim（nomination/None 拒绝）；`name`/`companyName` 必填（400）、名称/信用代码改撞他人 400（查重排除自身）；字段级合并更新（未提交项保留原值）+ `Merchant.Resubmit()` 清驳回原因。**v1.22.0**：请求体 `licenseUrl`→`documents[]`，矩阵按"存量已批准材料+本次新提交"合并判定（缺什么补什么） | `ResubmitMerchantApplication` |
| POST | `/api/merchant/{merchantId:guid}/delete-application` | **v1.21.0** 删除被驳回的入驻申请：仅 `Suspended` 且调用者是在职 MerchantAdmin；分支清理与撤回同构（`ApplicantApplicationCleanup` 共用）——Self 硬删商户+成员、Claim 解除认领人退回认领池、Nomination/None 拒绝 | `DeleteMerchantApplication` |
| POST | `/api/merchant/{merchantId:guid}/withdraw` | **v1.16.0** 申请人自助撤回待审核的入驻申请：仅 `Status=Pending` 且调用者是该商户在职 MerchantAdmin（申请人本人）可执行；按 `ApplicationMode` 分支清理——self 硬删商户+全部成员行、claim 软移除认领人成员并 `RevertClaimedToCrawler`（来源退回 Crawler、渠道归 None，重新进入认领池）、nomination/None 抛"该申请暂不支持自助撤回"。业务校验失败均映射 400 | `WithdrawMerchantApplication` |
| POST | `/api/merchant/{merchantId:guid}/close` | **v1.30.0** 商户自助关店：任一在职管理员发起；claim/提名已有→解除归属回公海（商品/证照/成员/邀请/纠错全清，Contact 隐私止血，DataSource 回 Crawler+Status→Pending）；self/提名新建→直接删除。幂等可重入，409/400 透传守卫原因 |
| POST | `/api/merchant/{merchantId:guid}/verify-request` | **v1.24.0** 商户主动申请认证（仅该商户在职管理员）：按材料矩阵即时校验（与 Admin 认证同口径），不齐 400 透传缺项引导；通过置 `VerifyRequested=true` 供 Admin 队列优先处理，重复提交幂等，Admin 认证后自动清除 | `RequestMerchantVerify` |

---

## 5. 用户端点 `/api/me`（24 个）

组授权：`RequireAuthorization("Authenticated")`

| 方法 | 路由 | 说明 |
|------|------|------|
| GET | `/api/me/profile` | 获取个人资料 |
| PUT | `/api/me/profile` | 更新个人资料 |
| POST | `/api/me/deactivate` | 注销当前账户（v2.14.0：唯一管理员商户拦截守卫、成员/邀请/申请/通知清理、Identity 禁用吊销、30 天冷静期后匿名化） |
| POST | `/api/me/avatar` | 上传头像（jpg/png/webp，≤2MB） |
| GET | `/api/me/roles` | 获取当前用户角色列表 |
| GET | `/api/me/permissions` | 获取当前用户权限键列表 |

### 5.1 收藏夹

| 方法 | 路由 |
|------|------|
| GET | `/api/me/favorites/bearings` |
| POST | `/api/me/favorites/bearings/{id}` |
| DELETE | `/api/me/favorites/bearings/{id}` |
| GET | `/api/me/favorites/bearings/{id}/check` |

### 5.2 关注商家

| 方法 | 路由 |
|------|------|
| GET | `/api/me/follows/merchants` |
| POST | `/api/me/follows/merchants/{merchantId:guid}` |
| DELETE | `/api/me/follows/merchants/{merchantId:guid}` |
| GET | `/api/me/follows/merchants/{merchantId:guid}/check` |

### 5.3 浏览历史

| 方法 | 路由 |
|------|------|
| GET | `/api/me/history/bearings` |
| POST | `/api/me/history/bearings/{bearingId:guid}` |
| GET | `/api/me/history/merchants` |
| POST | `/api/me/history/merchants/{merchantId:guid}` |
| DELETE | `/api/me/history/bearings/{id:guid}` |
| DELETE | `/api/me/history/merchants/{id:guid}` |
| DELETE | `/api/me/history/clear` |

### 5.4 纠错

| 方法 | 路由 |
|------|------|
| GET | `/api/me/corrections` | 获取当前用户的纠错列表 |
| GET | `/api/me/corrections/{id:guid}` | 获取纠错详情 |
| POST | `/api/me/bearings/{bearingId:guid}/corrections` |
| POST | `/api/me/merchants/{merchantId:guid}/corrections` |

---

## 6. 站内信端点 `/api/notifications`（6 个）

> v1.20.0 新增组。站内信为写扩散模型（Notifications 表，每收件人一行），由领域事件订阅者写入：
> 入驻审核通过/拒绝（通知商户在职管理员）、提名被接受（通知发起人）。
> 时间字段一律 UTC ISO 8601（带 Z），展示层转换本地时区。

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/notifications` | 收件箱分页列表，`unreadOnly/page/pageSize` 查询参数，返回 Paged |
| GET | `/api/notifications/unread-count` | 未读条数 `{ count }`（TabBar 角标轮询） |
| POST | `/api/notifications/{id:guid}/read` | 标记单条已读；非本人收件箱内通知返回 404 |
| POST | `/api/notifications/read-all` | 全部未读置已读，返回 `{ affected }` |
| DELETE | `/api/notifications/{id:guid}` | 删除单条（v1.27.0，硬删仅本人；消息中心左滑删除） |
| DELETE | `/api/notifications/read` | 清空全部已读（v1.27.0，未读保留；返回 `{ affected }`） |

---

## 7. 商家端点 `/api/merchant`（24 个）

组授权：`RequireAuthorization("Merchant")`。商家端点不通过 RequirePermission 校验，通过 ICurrentUserService 限定操作到当前商户范围（X-Merchant-Id 或首个在职成员商户）。

### 7.1 店铺管理

| 方法 | 路由 | 管理员 | 员工 |
|------|------|--------|------|
| GET | `/api/merchant/profile` | 可查看 | 可查看 |
| PUT | `/api/merchant/profile` | 可操作 | 拒绝 |
| POST | `/api/merchant/documents` | 可操作 | 拒绝 |
| POST | `/api/merchant/logo` | 可操作 | 拒绝 |

> v1.22.0：原 `POST /api/merchant/license` 泛化为下述三端点。

**v1.22.0 证照材料三端点**（原 `POST /api/merchant/license` 泛化，`LicenseVerification`→`MerchantDocument`）：

| 方法 | 路由 | 说明 |
|------|------|------|
| POST | `/api/merchant/documents` | multipart + form 字段 `type`（1/2/3），即时建待审记录进"商户文档审核"队列（入驻后换证/补材料通道）；同类已批准/待审执照防重复提交；绑定 `X-Merchant-Id` |
| GET | `/api/merchant/documents` | 当前商户全部材料与审核状态（信息维护页证照区数据源）；绑定 `X-Merchant-Id` |
| POST | `/api/merchant/documents/upload` | 材料文件预上传，只落盘返回 `{url}` 不建记录——入驻申请随单材料"先传后随单提交"通道 |

备注：`POST /api/merchant/documents`、`POST /api/merchant/documents/upload`、`POST /api/merchant/logo` 使用 `.DisableAntiforgery()`。

> v1.15.0 变更：`GET /api/merchant/profile` 改按 `X-Merchant-Id` 当前商户上下文定位（原按"首个在职成员"，多商户下读写错位）；`PUT /api/merchant/profile` 补 `IsMerchantAdmin` 校验（原仅成员即可）。`POST /api/merchant/logo` 上传 Logo（jpg/png/webp，≤2MB）落 `wwwroot/uploads/merchants/logo`，仅返回相对 URL，DB 写入由 `PUT /profile` 带 `logoUrl` 完成。

### 7.2 成员管理

| 方法 | 路由 | 管理员 | 员工 |
|------|------|--------|------|
| GET | `/api/merchant/staff` | 可查看 | 可查看 |
| POST | `/api/merchant/staff` | 可操作 | 拒绝 |
| DELETE | `/api/merchant/staff/{userId:guid}` | 可操作 | 拒绝 |
| POST | `/api/merchant/members/{userId:guid}/suspend` | 可操作 | 拒绝 |
| POST | `/api/merchant/members/{userId:guid}/activate` | 可操作 | 拒绝 |
| PUT | `/api/merchant/members/{userId:guid}/role` | 可操作 | 拒绝 |
| GET | `/api/merchant/staff/invitations/pending` | 本人 | 本人 |
| POST | `/api/merchant/staff/invitations/{invitationId:guid}/accept` | 本人 | 本人 |
| POST | `/api/merchant/staff/invitations/{invitationId:guid}/decline` | 本人 | 本人 |
| POST | `/api/merchant/staff/invitations/{invitationId:guid}/revoke` | 可操作 | 拒绝 |

> v1.14.0 说明：suspend（停用成员，保留关系但无操作权限）/ activate（恢复成员）/ role（变更 MerchantAdmin/MerchantStaff 角色）为新增端点，管理端操作均以 `permissionService.IsMerchantAdmin()`（查成员表 + 当前商户上下文）为准；移除最后一个 Active 管理员被守卫拦截。

> v1.25.0 说明（员工邀请确认制）：`POST /api/merchant/staff` 对已注册用户不再静默拉入，改为创建 `StaffInvitation(Type=Staff)` 待确认邀请并发站内信（响应 message 透传"邀请已发送，对方同意后加入"或"该用户已是…在职成员"）；未注册用户走原邀请码+短信/邮件通道。被邀人侧三端点（pending/accept/decline）以 JWT `phone_number`/`email` claim 服务端匹配"非空且相等"（不接受自报联系方式，排除 null==null 穿透），accept 建成员行并通知发起人；revoke 限该商户在职管理员。`GET /api/merchant/staff` 合并 `Status=Invited` 邀请行（`invitationId` 供撤销，成员行 `id` 为 Guid.Empty）。依赖 Identity 新增服务间端点 `GET /api/users/by-phone|by-email`（此前恒 404 是"添加成员无反应"根因）。

### 7.3 库存管理

| 方法 | 路由 | 管理员 | 员工 |
|------|------|--------|------|
| GET | `/api/merchant/bearings` | 可查看 | 可查看 |
| POST | `/api/merchant/bearings` | 可操作 | 可操作 |
| PUT | `/api/merchant/bearings/{id:guid}` | 可操作 | 可操作(仅自己添加的) |
| POST | `/api/merchant/bearings/{id:guid}/onshelf` | 可操作 | 可操作 |
| POST | `/api/merchant/bearings/{id:guid}/offshelf` | 可操作 | 可操作 |
| PUT | `/api/merchant/bearings/{id:guid}/price-visibility` | 可操作 | 可操作 |
| GET | `/api/merchant/inventory/import-context` | 可判权 | 拒绝 |

---

## 8. 后台管理端点 `/api/admin`（72 个）

组授权：`RequireAuthorization("Admin")`。所有端点通过 `RequirePermission("bearing.xxx")` 或 `RequireRole("SuperAdmin,Admin")` 控制访问。

### 8.1 仪表盘

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/dashboard/stats` | `dashboard.view` |

### 8.2 轴承管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/bearings` | `bearing.view` |
| POST | `/api/admin/bearings` | `bearing.create` |
| PUT | `/api/admin/bearings/{id:guid}` | `bearing.edit` |
| DELETE | `/api/admin/bearings/{id:guid}` | `bearing.delete` |
| PUT | `/api/admin/bearings/{id:guid}/restore` | `data.restore` |
| DELETE | `/api/admin/bearings/{id:guid}/hard` | `data.harddelete` |

### 8.3 品牌管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/brands` | `bearing.view` |
| POST | `/api/admin/brands` | `bearing.create` |
| PUT | `/api/admin/brands/{id:guid}` | `bearing.edit` |
| DELETE | `/api/admin/brands/{id:guid}` | `bearing.delete` |
| PUT | `/api/admin/brands/{id:guid}/restore` | `data.restore` |
| DELETE | `/api/admin/brands/{id:guid}/hard` | `data.harddelete` |

### 8.4 轴承类型管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/bearing-types` | `bearing.view` |
| POST | `/api/admin/bearing-types` | `bearing.create` |
| PUT | `/api/admin/bearing-types/{id:guid}` | `bearing.edit` |
| DELETE | `/api/admin/bearing-types/{id:guid}` | `bearing.delete` |
| PUT | `/api/admin/bearing-types/{id:guid}/restore` | `data.restore` |
| DELETE | `/api/admin/bearing-types/{id:guid}/hard` | `data.harddelete` |

### 8.5 商家管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/merchants` | `merchant.view` |
| GET | `/api/admin/merchants/{id:guid}` | `merchant.manage` |
| POST | `/api/admin/merchants` | `merchant.manage` |
| PUT | `/api/admin/merchants/{id:guid}` | `merchant.manage` |
| DELETE | `/api/admin/merchants/{id:guid}` | `merchant.manage` |
| PUT | `/api/admin/merchants/{id:guid}/restore` | `data.restore` |
| DELETE | `/api/admin/merchants/{id:guid}/hard` | `data.harddelete` |
| POST | `/api/admin/merchants/{id:guid}/approve` | `merchant.verify` |
| POST | `/api/admin/merchants/{id:guid}/members` | `merchant.verify` |
| POST | `/api/admin/merchants/{id:guid}/verify` | `merchant.verify` |
| POST | `/api/admin/merchants/{id:guid}/reject` | `merchant.verify` |

> `GET /api/admin/merchants` 支持查询参数：keyword（名称/公司名搜索）、city、type（MerchantType）、verifiedOnly（仅认证商家）、status（MerchantStatus：0=Active、1=Suspended、2=Pending、3=Draft）、excludeCrawler（bool，排除爬虫来源商家，仅显示入驻申请商家）、includeDeleted、page、pageSize。

> v1.14.0 说明：
> - `POST /api/admin/merchants/{id}/approve`：审核通过入驻申请，商户 Pending → Active 生效（补齐原缺失的审核门）；与 verify 认证分离。
> - `POST /api/admin/merchants/{id}/members`：平台指定商户成员（商户无在职管理员等异常时兜底指定/恢复成员，已存在成员行则恢复/改角色，否则新建；复用 merchant.verify 权限）。
> - `POST /api/admin/merchants/{id}/verify` 仅标记商户为已认证，不再清除爬虫 MerchantBearing 数据。

> v1.20.0 说明：
> - `approve`/`reject` 增加并发守卫：商户当前状态非 Pending（另一管理员已先处理）时返回 **409** ProblemDetails，`code=MERCHANT_ALREADY_PROCESSED`、`currentStatus` 携带实际状态，Admin 前端据此提示并刷新列表。
> - 两操作成功后经领域事件（`MerchantApprovedEvent`/`MerchantRejectedEvent`）向商户在职管理员写站内信（见第 6 组）。
> - `GET /api/admin/merchants` 列表项与 `GET /api/admin/merchants/{id}` 详情响应补 `applicationMode`（入驻渠道）、`submittedAt`（提交时间）、`rejectReason`（拒绝原因）三字段；同时修复列表项 `status` 恒为空的映射缺陷。

### 8.6 证照材料管理（v1.22.0 由"营业执照管理"泛化，路由 `/licenses/*`→`/documents/*`）

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/documents/pending` | `merchant.verify` |
| POST | `/api/admin/documents/{id:guid}/approve` | `merchant.verify`（仅批准该条材料，**不再自动认证**——认证改走 verify 按材料矩阵口径） |
| POST | `/api/admin/documents/{id:guid}/reject` | `merchant.verify`（v1.22.0 起 body 可带 `reason`） |
| GET | `/api/admin/merchants/{id:guid}/documents` | `merchant.verify`（**v1.22.0 新增**：审批抽屉"申请材料"区数据源，全状态材料） |

### 8.7 纠错管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/corrections` | `correction.review` |
| GET | `/api/admin/corrections/pending` | `correction.review` |
| GET | `/api/admin/corrections/{id:guid}` | `correction.review` |
| POST | `/api/admin/corrections/{id:guid}/approve` | `correction.review` |
| POST | `/api/admin/corrections/{id:guid}/reject` | `correction.review` |

### 8.8 用户角色管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| POST | `/api/admin/users/{userId:guid}/roles` | `user.manage` |
| DELETE | `/api/admin/users/{userId:guid}/roles/{roleName}` | `user.manage` |
| GET | `/api/admin/users/{userId:guid}/roles` | `user.manage` |
| GET | `/api/admin/users/{userId:guid}/permissions` | `user.manage` |

### 8.9 角色管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/roles` | `role.manage` |
| GET | `/api/admin/roles/{id:guid}` | `role.manage` |
| GET | `/api/admin/roles/all` | `role.manage` |
| POST | `/api/admin/roles` | `role.manage` |
| PUT | `/api/admin/roles/{id:guid}` | `role.manage` |
| DELETE | `/api/admin/roles/{id:guid}` | `role.manage` |
| POST | `/api/admin/roles/{id:guid}/permissions` | `role.manage` |
| GET | `/api/admin/roles/{id:guid}/permissions` | `role.manage` |

### 8.10 权限管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/permissions` | `role.manage` |
| GET | `/api/admin/permissions/{id:guid}` | `role.manage` |
| POST | `/api/admin/permissions` | `role.manage` |
| PUT | `/api/admin/permissions/{id:guid}` | `role.manage` |
| DELETE | `/api/admin/permissions/{id:guid}` | `role.manage` |

### 8.11 审计日志

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/audit-logs` | `audit.view` |

### 8.12 系统配置

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/config` | `system.view` |
| PUT | `/api/admin/config/{key}` | `system.manage` |
| POST | `/api/admin/cache/refresh-rate-limit` | `system.manage` |
| GET | `/api/admin/config/price` | `system.view` |

---

## 9. 寻货端点 `/api/sourcing`（11 个）

| 方法 | 路径 | 说明 | 鉴权 |
|---|---|---|---|
| GET（v1.46.0 同版追加：输出 publisherMerchantId/publisherMerchantName/publisherType） | 匿名 |
| GET；v1.46.0 同版追加：透出发布方身份三字段，选定解锁的 publisherContact 分流——商户单=商户公开电话，个人单=注册手机 | 登录可选 |
| POST | /demands | 发布寻货（免费 3 条/日，超限 NEED_POINTS 协议 + usePoints 重提交，硬上限 10；v1.46.0 同版追加：请求体可选 merchantId=商户名义发布，在职成员校验前置不耗额度；v1.51.0：响应回传匹配家数，v1.54.0 更名为 `notifiedMerchantCount`=本次实际推送的认证商家家数，0=仅进大厅等应答；v1.54.0：请求体 `visibility`（0公开招标/1邀标）+ `invitedMerchantIds`（邀标收件人，空=全部匹配的认证在售商家）取代原 `targetMerchantId`；邀标勾选 ∩ 认证在售匹配，交集空则拒绝） | 登录 |
| POST | /demands/{id:guid}/publish-public | 转为公开招标（v1.54.0：发布人把进行中的邀标单放开为公开，需求进大厅人人可见可应答；已是公开或已结束的不予受理；不追加通知） | 登录 |
| POST | /demands/{id:guid}/respond | 商户应答（v1.45.0 多行标书：请求体 `items[]` 每行型号必填+可引在售商品，说明 remark 整份必填，至少一行；一商户一需求单条可更新=整份替换行；免费 20/日超限花积分，硬上限 50） | 登录+商户 |
| DELETE | /demands/{id:guid}/respond | 撤销应答（同版本追加：招投标"开标前撤标"——仅待处理可撤，撤后需求回到未应答，行表 DB 级联删除；仅本人+在职成员，当日额度不退还） | 登录+商户 |
| POST | /demands/batch-delete | 批量删除寻货（v2.12.0 列表多选：软删终态单，仅经办人本人，进行中拒删计 skipped） | 登录 |
| POST | /demands/{id:guid}/select | 选定应答（关闭需求、其余置未选中、双方解锁联系方式） | 登录+发布人 |
| POST | /demands/{id:guid}/cancel | 取消寻货（软关闭，通知全体应答者） | 登录+发布人 |
| GET | /my/demands | 我发布的寻货（v1.46.0 同版追加：只含个人名义单——商户名义归 merchant/demands） | 登录 |
| GET | /merchant/demands | 商户名义发布的寻货列表（v1.46.0 同版追加：商户工作台"寻货管理-我发布的"，当前商户全状态单+经办人，在职成员均可见） | 登录+商户 |
| GET | /my/responses | 当前商户的应答记录（v1.45.0 透出 `items` 多行型号） | 登录+商户 |
| GET | /quota | 额度聚合（发布/应答免费额度、今日已用、硬上限、积分单价、余额——额度条数据源，v1.35.0） | 登录 |
| GET | /my-offering | 我的在售同款（当前商户对该型号的在售/补货中条目，应答表单预填数据源，v1.37.0） | 登录+商户 |
| GET | /opportunities | 需求信号（商户在售型号中被寻货且未应答的聚合清单，反向导购横幅数据源，v1.37.0） | 登录+商户 |

## 10. 积分端点 `/api/points`（4 个）

| 方法 | 路径 | 说明 | 鉴权 |
|---|---|---|---|
| GET | /account | 余额/累计/连签天数 | 登录 |
| POST | /checkin | 每日签到（阶梯 [2,3,4,5,5]） | 登录 |
| GET | /transactions | 收支明细分页 | 登录 |
| GET | /tasks | 赚分任务清单（启用规则+完成态：daily 看今日/once 看历史；含今日次数与上限，v1.34.0） | 登录 |

## 11. 端口配置

| 环境 | HTTP | HTTPS |
|------|------|-------|
| 开发 | 5183 | 7183 |
| 生产 | 8080 | — |

---

## 12. 健康检查端点

| 端点 | 说明 |
|------|------|
| `/health` | 详细 JSON 健康检查 |
| `/healthz` | 纯文本健康检查 |
| `/health/live` | K8s 存活探针 |
| `/health/ready` | K8s 就绪探针 |

---

## 本版增量（v1.40.0）：成就/商城/工会经济端点（22 个）

### 成就（/api/achievements，登录用户）
| 方法 | 路径 | 说明 |
|---|---|---|
| GET | /api/achievements/wall | 成就墙（定义+本人解锁态与进度） |
| GET | /api/achievements/me | 我的已解锁徽章排 |
| GET | /api/achievements/merchants/{id} | 商户徽章排（商户详情信任信号） |
| GET | /api/admin/achievements | Admin 成就目录全量（system.manage） |
| PUT | /api/admin/achievements/{id} | Admin 编辑成就（分值/阈值/称号/启停，system.manage） |

### 商城（/api/mall 用户 + /api/admin/mall 管理）
| 方法 | 路径 | 说明 |
|---|---|---|
| GET | /api/mall/items | 商城目录（生效价/闪购态/库存/礼品归属商户名+本人余额） |
| POST | /api/mall/redeem | 虚拟权益兑换（body: itemId/targetRef/requestId/useTreasury；金库通道仅商户管理员） |
| POST | /api/mall/redeem-gift | 实物礼品托管兑换（收货三件套必填；自兑排除+同址同机月限3） |
| POST | /api/mall/orders/{id}/confirm-receipt | 确认收货（触发金库结算 settle:{订单} 幂等） |
| GET | /api/mall/orders | 我的兑换订单分页（含物流态） |
| GET | /api/admin/mall/items | 目录全量含停用（system.manage） |
| PUT | /api/admin/mall/items/{id} | 编辑商品（价格/闪购窗口 UTC/库存/上下架，system.manage） |
| GET | /api/admin/mall/gifts/pending | 待审商家挂礼队列（merchant.verify） |
| POST | /api/admin/mall/gifts/{id}/approve | 挂礼审核定档（平台定价 ≤Business.GiftPriceCeiling，merchant.verify） |
| POST | /api/admin/mall/gifts/{id}/reject | 挂礼驳回（原因商户可见，merchant.verify） |
| GET | /api/admin/mall/orders/escrow | 托管中实物礼品单（merchant.manage） |
| POST | /api/admin/mall/orders/{id}/refund | 争议退款（仅未结算单原路退分，merchant.manage） |

### 商家金库与挂礼（/api/merchant，X-Merchant-Id 上下文，业务资格鉴权零 RBAC 键）
| 方法 | 路径 | 说明 |
|---|---|---|
| GET | /api/merchant/treasury | 金库余额/累计（仅管理员） |
| GET | /api/merchant/treasury/transactions | 金库流水分页（仅管理员） |
| POST | /api/merchant/gifts | 申请挂礼（仅管理员，进待审默认下架） |
| GET | /api/merchant/gifts | 我的挂礼列表含审核态（仅管理员） |
| POST | /api/merchant/gifts/{id}/offshelf | 下架礼品（仅管理员，存量订单不受影响） |
| POST | /api/merchant/gifts/image | 礼品图上传（对象存储相对 URL，仅管理员，5MB 图片） |
| GET | /api/merchant/gift-orders | 礼品订单列表（在职成员，可按 shipStatus 过滤，含收货信息） |
| POST | /api/merchant/gift-orders/{id}/ship | 发货登记（在职成员，物流单号必填，起算7天自动确认） |

> 金库四场景 member_trickle/gift_settlement/treasury_spend/treasury_burn；六参数 SystemConfig 键（Business.TricklePercent=10 等）见《11-积分体系设计 v2.4.0》。

---

## 本版增量（v1.41.0）：M2 工会等级与福利卡

| 方法 | 路径 | 说明 |
|---|---|---|
| GET | /api/points/guild-buff | 工会福利卡：成员最佳工会等级/被动加成清单/升级提示（散人 rank=0 空清单） |

> 变更字段：/api/merchant/treasury 响应新增 grade/gradeDisplay；商户详情 DTO 新增 GradeDisplay（入驻/认证/活跃供给/金牌）。无新表新列，三阈值配置键（Business.GuildPremiumOnSaleMin/GuildGoldOnSaleMin/GuildGoldTreasuryMin）并入迁移 AddMerchantTreasury 种子。端点总数 189→190。

---

## 本版增量（v1.42.0）：M3 商家集体任务与排行 + 术语统一（承《11-积分体系设计 v2.6.0》）

> 术语统一：原"工会"全链路改名"商家"。/api/points/guild-buff -> /api/points/merchant-buff（响应字段 guildId/guildName -> merchantId/merchantName），
> 三阈值配置键 Business.Guild* -> Business.Merchant*（AddMerchantTreasury 未发布种子直接改）。

| 方法 | 路径 | 说明 |
|---|---|---|
| GET | /api/points/merchant-tasks | 商家集体任务板：最佳商家的周期任务进度与完成态 + completedTotal 累计达标（散人空清单） |
| GET | /api/points/merchant-ranking | 商家实力月榜：本月金库入账 TOP20 + 我的商家回显（rank=0 未上榜） |
| GET | /api/admin/points/merchant-tasks | 集体任务定义全量列表（points.manage） |
| POST | /api/admin/points/merchant-tasks | 新建任务定义（TaskKey 全局唯一、创建后不可变，points.manage） |
| PUT | /api/admin/points/merchant-tasks/{id} | 编辑任务定义（目标/周期/奖励/启停实时生效，points.manage） |

> 新表 MerchantTaskDefinitions / MerchantTaskCompletions（迁移 AddMerchantTasks，含三条任务种子 + merchant_task 发放规则种子）；
> 个人任务清单端点过滤集合新增 merchant_task（集体任务被动发放不进个人任务列表）。端点总数 190->195。

> v1.42.0 追加（商家主页批次）：GET /api/points/merchant-tasks 支持 ?merchantId=（校验请求者在职成员，防窥探他店任务板）；
> GET /api/merchants/{id} 响应新增 isMerchantMember/memberRole/completedTaskCount 三字段（登录透传身份，匿名保持缺省）。端点总数不变。

> v1.42.0 再追加（任务中心拆分+归属回溯）：GET /api/points/merchant-buff 与 /api/points/merchant-ranking 支持 ?merchantId=
> （商家管理页"本店视角"，校验在职成员）；商家详情 isMerchantMember 保留、memberRole 删除（角色走成员列表接口，详情不冗余）；
> 集体任务 corrections 指标改按成员任职区间 [JoinedAt,RemovedAt] 回溯计数（离职者在职贡献不丢，Rejoin 多段任职为已知盲区）。

---

## 本版增量（v1.48.0）：商家商品审核 + 状态统一 + 替代品三级治理（+13）

> v1.52.0 更新：本节 ③ 替代品三级治理中的 **`GET /api/admin/interchanges/pending`、`POST .../approve`、`POST .../reject`、`POST /api/merchant/interchanges`、`GET /api/merchant/interchanges` 五个端点已移除**（替代品字典化，见文末 v1.52.0 章节）；三级类型标签（标准等效/条件替代/仅供参考）与公域查询保留。

### 商家商品审核（/api/admin/merchant-bearings，merchant.verify）
| 方法 | 路径 | 说明 |
|---|---|---|
| GET | /api/admin/merchant-bearings/pending | 待审商品队列（审核位 reviewStatus=Pending） |
| POST | /api/admin/merchant-bearings/{id:guid}/approve | 审核通过（→已通过 + 出售中，公域可见） |
| POST | /api/admin/merchant-bearings/{id:guid}/reject | 审核驳回（附理由，商户可见、可改重提） |

### 商家商品状态契约（/api/merchant/bearings）
| 方法 | 路径 | 说明 |
|---|---|---|
| DELETE | /api/merchant/bearings/{id:guid} | 商家删除商品：出售中/补货中禁删（须先下架），其余状态物理删除 |

> 字段变更：`MerchantBearingDto` 由 `isOnSale`/`isRestocking`/`isPendingApproval`/`isFeatured`（后者僵尸字段，已删）收敛为
> `saleState`（0已下架/1出售中/2补货中）+ `reviewStatus`（0仓库中/1审核中/2已通过/3已驳回）。
> 商家"上架"语义改为：未经审核→提交审核（→审核中，公域暂隐），已通过→直接出售中。
> 公域三面（商城 feed `SearchMallFeedAsync`、轴承商家列表 `GetMerchantsByBearing`、店铺页 `GetMerchantBearingsByMerchant`）统一过滤 `reviewStatus=已通过 且 saleState∈{出售中,补货中}`。

### 替代品三级治理（/api/admin/interchanges，读 interchange.view / 写 interchange.manage）

> 权限（v1.53.0 起）：列表查询（GET）需 `interchange.view`；新建/编辑/删除（POST/PUT/DELETE）需 `interchange.manage`。此前借用 `merchant.verify`（认证审核语义），替代关系独立为主数据权限键后不再借用。权限目录 32→34 键，迁移 `AddInterchangePermissions`。

| 方法 | 路径 | 说明 |
|---|---|---|
| GET | /api/admin/interchanges | 替代关系列表（关键字/创建时间/来源过滤，分页） |
| POST | /api/admin/interchanges | 平台新建替代关系（源/目标型号 + 类型 + 置信度 + 双向，直接生效） |
| PUT | /api/admin/interchanges/{id:guid} | 编辑替代关系（类型/置信度/来源/备注/双向） |
| DELETE | /api/admin/interchanges/{id:guid} | 删除替代关系（物理删除） |
| ~~GET /api/admin/interchanges/pending~~ | **已于 v1.52.0 移除**（替代品字典化，无候选流） |
| ~~POST /api/admin/interchanges/{id:guid}/approve~~ | **已于 v1.52.0 移除**（同上） |
| ~~POST /api/admin/interchanges/{id:guid}/reject~~ | **已于 v1.52.0 移除**（同上） |

### 商家替代候选（/api/merchant/interchanges）

> **已于 v1.52.0 整组移除**（替代品字典化）：`POST /api/merchant/interchanges`（提交候选）、`GET /api/merchant/interchanges`（我的提交）。
> 商家/用户的参与通道改为**纠错**字段 `interchange`（详见文末 v1.52.0 章节）。

### 公域查询（变更）
| 方法 | 路径 | 说明 |
|---|---|---|
| GET | /api/bearings/{id:guid}/interchanges | 轴承替代品列表（v1.52.0 起返回全部生效关系——审核态已取消；返回含 `typeLabel` 的富 DTO） |

> 三级替代类型：`exact`=标准等效替代 / `conditional`=条件替代（需评估）/ `functional`=仅供参考（不进自动推荐）。
> 双向语义修复：查询改"源 ∪ 目标"合并，`isBidirectional=true` 的反向替代也可见（带方向标注）。
> 迁移 `AddBearingSaleReviewState`（商品两轴状态收敛 + 回填 + 删四旧列）、`AddInterchangeReview`（替代审核位，存量行默认已生效保持可见；**后者已由 v1.52.0 的 `RemoveInterchangeReview` 撤销**）。
> 配套文档：《15-数据状态设计 v1.3.0》《16-替代品治理设计 v2.0.0》。

---

## 本版增量（v1.49.0）：搜索—寻货—认证商家与数据可信联动（+2）

### 通知偏好（/api/notifications，登录用户）
| 方法 | 路径 | 说明 |
|---|---|---|
| GET | /api/notifications/preferences | 读取通知偏好：渠道开关 + `smsGatewayReady`（短信网关就绪标记，未就绪前端隐藏短信开关） |
| PUT | /api/notifications/preferences | 更新通知偏好（sms/weChat/email 三项，null=不改；站内信恒开不在范围） |

### 寻货联动（/api/sourcing，登录用户）
| 方法 | 路径 | 说明 |
|---|---|---|
| POST | /api/sourcing/demands | 发布请求体加可选 `targetMerchantId`：定向询价（仅推目标商户、**不进公开大厅**） |
| GET | /api/sourcing/matched | 商家侧"为你匹配的寻货"：定向给我的 + 匹配我在售型号（bearingId 精确，回退型号文本）的进行中广播单 |

### 字段/契约变更（无端点数量变化）
| 对象 | 变更 |
|---|---|
| `GET /api/sourcing/demands/{id}` | 输出新增 `isTargeted`（定向单标记） |
| `BearingDto` | 新增 `DataSource`（Internet/OcrExtract/SeedData=仅供参考 → 前端标"参数来源：互联网 · 仅供参考"） |
| `BearingMerchantDto`（`GET /api/bearings/{id}/merchants`） | 新增 `IsVerified`（询价行动门"仅认证商家可询价"依据） |
| `MerchantDto`（公共查询） | **移除** `Phone/Mobile/Email`（电话单轨：号码只在寻货撮合达成后互给） |
| `MerchantDetailDto` | contact 改嵌套 `Contact`（仅本商户成员自看/工作台下发；公共查询不下发） |

> 发布即触达：新增领域事件 `SourcingDemandPublishedEvent` → 订阅者匹配"在售本轴承（公域可见）的认证商家"（定向单只推目标商户；
> 广播单无 `bearingId` 时**按型号文本精确匹配轴承主数据兜底**，故发现页直接发布亦能触达有货商家），
> 多渠道通知（站内信必发 + 短信/微信/邮件按用户偏好扇出，网关未就绪只记日志），限频 `Sourcing.NotifyMaxPerMerchantPerDay`（默认 3）。
> 深链修复：寻货通知 `bizType` 由字面量 `"SourcingDemand"` 改常量 `Notification.BizSourcingDemand`（`sourcing_demand`），前端消息中心路由到寻货详情。
> 应答分发：**商户名义**发布的寻货收到应答时通知该商户**全部在职成员**（此前仅经办人；站内信必发 + 外部按各自偏好），**个人名义**仅通知发布人。
> 预留 `MatchCompletedEvent`（撮合达成=互给电话，供后续交易域订阅；开源侧不含实现）。
> 迁移 `AddSourcingTargetMerchant`（SourcingDemands 加 `TargetMerchantId` + 索引）。
> 僵尸清理：删 `GetMerchantByUserIdQuery`/`GetMerchantByUserIdQueryHandler`；`MerchantExtensions.ToDetailDto` 移除仅用于屏蔽 contact 的 `isAuthenticated` 分支。
> 端点总数 189 → 191（"为你匹配"与通知偏好共 +2；明细以本表为准）。
> 配套文档：《搜索寻货与数据可信联动设计 v1.0.0》。

---

## 本版增量（v1.50.0）：仪表盘"新增"按来源拆分 + 商家入驻时间（端点数量不变）

> 仅新增/调整字段与查询参数，端点数量不变。

### 新增领域字段
| 对象 | 变更 |
|---|---|
| `Merchant.OnboardedAt` | 新增（`DateTime?`，UTC）：入驻生效时间，`Merchant.Approve()` 内 `??=` 幂等写入（Self/Claim/Nomination 三条入驻路径都经该方法；**认领既有互联网商户也能正确计时**）。迁移 `AddMerchantOnboardedAt`（加列 + 回填：`Status∈{Active,Suspended}` 且 `ApplicationMode≠None` 的行取 `CreatedAt`） |
| `MerchantDto.onboardedAt` | 下发入驻生效时间（null=尚未入驻） |

### 仪表盘统计（`GET /api/admin/dashboard/stats`，`dashboard.view`）
| 对象 | 变更 |
|---|---|
| `bearings.internetAdded` / `bearings.manualAdded` | 新增：轴承新增按来源拆分（`{ today, week, month }`）；互联网=`DataSource=Internet`，人工=非互联网 |
| `merchants.internetAdded` / `merchants.onboarded` | 新增：商家新增按来源拆分（`{ today, week, month }`）；互联网=`DataSource=Internet` 且 `CreatedAt≥起点`，入驻=`OnboardedAt≥起点`（**含认领既有商户**） |
| `merchants.onboardedCount` | 新增：已入驻商户数（`OnboardedAt` 非空），仪表盘卡片"已入驻"真值（此前误用 `verifiedCount`） |
| `interchanges` | 新增统计块：`totalCount` / `internetAdded` / `manualAdded`（`{ today, week, month }`）。**v1.52.0 删除 `pendingCount`**（无候选待审，恒为 0） |
| `periods` | 新增顶层对象 `{ today, weekStart, monthStart }`（业务日历 `yyyy-MM-dd` 起点），供前端图表下钻使用同一日界 |
| 移除 | `bearings.todayAdded/thisWeekAdded/thisMonthAdded`、`merchants.todayRegistered/thisWeekRegistered/thisMonthRegistered`（改由上述拆分字段承载） |

### 商品/商家列表查询参数（`GET /api/admin/merchants`，`merchant.view`）
| 参数 | 说明 |
|---|---|
| `registeredFrom` / `registeredTo` | 注册时间区间（业务日历 `yyyy-MM-dd`，两端含；按 `BusinessClock` 日界换算为 UTC 区间，上限取"次日零点不含"） |
| `onboardedFrom` / `onboardedTo` | 入驻时间区间（同上口径，过滤 `OnboardedAt`） |
| `internetSource` | 来源筛选：`true`=仅互联网来源；`false`=仅非互联网来源；不传=不限（来源拆分行下钻用） |
| `sortBy` | 取值新增 `onboardedat`（按入驻时间排序；`registeredat` 为注册时间） |
| `sortOrder` | 与 `sortBy` 搭配的 `asc`/`desc` |

> 口径说明：日期一律按业务日历（`BusinessClock`，默认 UTC+8）解释并由 **API 统一换算为 UTC 区间**，前端/Admin 不做时区自算。
> 用途：仪表盘拆两张图——**互联网新增**（爬虫/同步）与**人工新增**（平台维护 / 商家入驻），各含 轴承/商家/替代品 三系列；
> 点"商家"柱按 `periods` 起点下钻：互联网图 → `/Data/Merchants?dateField=registered&dateFrom=<起点>&sourceFilter=internet`，
> 人工图 → `/Data/Merchants?dateField=onboarded&dateFrom=<起点>`。
> 各实体来源口径：轴承=创建时 `DataSource`；商家=互联网为 `DataSource`，人工为**入驻生效**（含认领）；替代品=互联网为 `DataSource`（管线推送），人工为平台维护（Manual）。

### 轴承列表查询参数（`GET /api/admin/bearings`，`bearing.view`）
| 参数/字段 | 说明 |
|---|---|
| `createdFrom` / `createdTo` | 创建时间区间（业务日历 `yyyy-MM-dd`，两端含；同商家口径换算为 UTC 区间） |
| `internetSource` | `true`=仅互联网来源；`false`=仅非互联网来源；不传=不限 |
| `BearingDto.createdAt` | 新增：创建时间（UTC），轴承列表展示"创建时间"列 |

### 替代品列表查询参数（`GET /api/admin/interchanges`，`interchange.view`）
| 参数 | 说明 |
|---|---|
| `createdFrom` / `createdTo` | 创建时间区间（业务日历 `yyyy-MM-dd`，两端含） |
| `internetSource` | `true`=仅互联网来源（数据管线批量）；`false`=仅非互联网来源（平台维护） |

> 三端下钻一致：轴承柱 → `/Data/Bearings?dateFrom=…&sourceFilter=internet|noninternet`；
> 替代品柱 → `/Interchanges?createdFrom=…&sourceFilter=internet|noninternet`；商家柱见上。
> 注：未启用互联网数据管线的部署，「互联网新增」系列恒为 0，仅「人工新增」有数据。

---

## 本版增量（v1.51.0）：寻货发布回传匹配家数（端点数量不变）

| 对象 | 变更 |
|---|---|
| `POST /api/sourcing/demands` 响应 | 由 `{ id }` 扩为 `{ id, matchedMerchantCount }`：本次发布**已匹配并推送**的认证商家家数（广播单按在售匹配口径、定向单=1、无匹配=0） |

> 背景：轴承详情此前只有「发起寻货」一个入口，用户看不出"有在售 → 直接推给这些商家"与"无在售 → 进大厅等应答"两条路径的差别，也看不出"在售"≠"可询价"。
> 口径统一：匹配逻辑抽为 `SourcingMatcher.GetMatchedMerchantIdsAsync`（公域可见在售本轴承 → 去重商户 → 仅认证商家；无 `bearingId` 时按型号文本精确匹配主数据兜底），
> 发布处理器（回传家数）与 `SourcingDemandPublishedNotificationHandler`（逐户推送）同源，保证"回传家数"与"实际推送对象"永远一致。
> 前端消费：Taro 发布页按家数差异化反馈（定向="已定向推送给该商家"；广播匹配>0="已通知 N 家认证商家"；=0="已进入寻货大厅"）；轴承详情按钮/路径说明文案按认证在售数自适应。
> 行为边界不变：广播单仍只有登录门槛（不加"仅认证在售才可发起"），触达范围仍固定为"在售本轴承的认证商家"（故无需再单独发一轮定向——一条广播即覆盖两条路径）。
> 配套文档：《搜索寻货与数据可信联动设计 v1.1.0》。

---

## 本版增量（v1.53.0）：替代关系独立权限键（端点数量不变）

| 端点 | 原权限 | 现权限 |
|---|---|---|
| `GET /api/admin/interchanges` | `merchant.verify` | `interchange.view` |
| `POST /api/admin/interchanges` | `merchant.verify` | `interchange.manage` |
| `PUT /api/admin/interchanges/{id:guid}` | `merchant.verify` | `interchange.manage` |
| `DELETE /api/admin/interchanges/{id:guid}` | `merchant.verify` | `interchange.manage` |

> 背景：替代关系自 v1.52.0 起已是与轴承/品牌/型号同级的平台主数据（字典），继续借用 `merchant.verify`（认证审核语义）名实不符，且无法把"查看"与"维护"两档能力分别授予（如审计员只读看、操作员可维护）。
> 变更：目录新增 `interchange.view`（查看替代关系，`type.delete` 之后）、`interchange.manage`（维护替代关系），键数 32→34。
> 授权：Admin 全量；Operator 增 `interchange.view` + `interchange.manage`（18→20）；Auditor 增 `interchange.view`（11→12）；Individual 仍 0。
> 迁移：`AddInterchangePermissions`（幂等补插 2 键 + 按角色名子查询授权，Admin 全量 / Operator 查看 + 维护 / Auditor 仅查看；Down 先删授权再删键）。
> 配套文档：《16-替代品治理设计 v2.0.1》，Admin 侧《认证集成设计 v1.2.3》《Admin 总体设计 v1.7.3》《Admin 功能需求与后端服务映射 v1.43.0》。

---

## 本版增量（v1.54.0）：寻货公开招标 / 邀标（+1 端点）

| 端点 | 变更 |
|---|---|
| `POST /api/sourcing/demands` | 请求体 `targetMerchantId`（v1.49.0 引入）→ 改 `visibility`（0公开招标/1邀标）+ `invitedMerchantIds`（`uuid[]`，空=全部匹配的认证在售商家）；响应 `matchedMerchantCount` → **更名 `notifiedMerchantCount`**（语义：本次实际推送的认证商家家数；公开=匹配家数、邀标=勾选∩匹配家数、无匹配=0） |
| `POST /api/sourcing/demands/{id:guid}/publish-public` | **新增**：发布人把进行中的邀标单放开为公开（进大厅，人人可见可应答）；已是公开/已结束 → 400 |
| `GET /api/sourcing/demands`（大厅） | 只列 `Visibility=Public` 的公开单；邀标单不进大厅 |
| `GET /api/sourcing/demands/{id:guid}` | 邀标单仅发布人与收件商家可见（非收件商家 → 404）；输出加 `visibility`/`isPrivate`（`isTargeted` 弃用） |
| `GET /api/sourcing/my/demands`、`/merchant/demands`、`/matched`、`/opportunities` | 输出加 `isPrivate`；`/matched` 增列"邀标给我的进行中单"（收件商家可见） |
| `GET /api/admin/sourcing/demands`、`/demands/{id:guid}` | 治理视图输出加 `isPrivate`/`invitedCount`（详情含 `invitedMerchantIds`），Admin 寻货管理列表/详情展示「发布方式」 |

> 背景：原 `targetMerchantId` 单字段同时承担"曝光范围"与"收件人"两个语义，且把"询价"抬成与寻货平级的单据类型；实际领域模型中价格只是应答行的可选字段、**选定**才是终局动作。v1.54.0 拆成两个正交轴。
> 领域事件：`SourcingDemandPublishedEvent` 改为携带**最终收件人集合**（`RecipientMerchantIds`），命令处理器解析一次（`SourcingMatcher` 匹配 ∩ 勾选），订阅者按集合逐户推送——从根上消除"回传家数"与"实际推送对象"漂移。
> 规则：无在售认证商家（匹配数为 0）时**强制公开招标**（邀标请求直接 400）；公开招标与邀标共用同一收件人基数（在售本轴承的认证商家）。
> 迁移：`AddSourcingVisibility`（加列 + gin 索引；存量 `TargetMerchantId` 非空行回填 `Visibility=1`、`InvitedMerchantIds=[该商户]`，随后删列）。
> 配套文档：《搜索寻货与数据可信联动设计 v1.2.0》《通知消息体系设计 v1.1.0》。

---

## 本版增量（v1.55.0）：邀标边界补全与契约纠正（端点数量不变）

| 端点 | 变更 |
|---|---|
| `POST /api/sourcing/demands/{id:guid}/respond` | **邀标守卫**：仅收件商家（发布时点快照命中）可应答/更新，其余商户报错；公开单不限制 |
| `GET /api/sourcing/demands/{id:guid}` | 可见性判定由"发布人本人 / 当前选中商户∈收件人"放宽为"发布商户在职成员（`viewerMerchantIds` 含 `PublisherMerchantId`）/ 任一收件商户在职成员"——修一人多商户深链与发布商户同事误 404；发布方视图（全量应答明细）随之放宽为发布方成员可见 |
| `POST /api/sourcing/demands` | 响应 `notifiedMerchantCount` → **更名 `recipientMerchantCount`**（收件人集合大小；实际投递还取决于收件商户是否有在职管理员）；`visibility` 枚举范围校验（非法值拒绝，原会生成"既不在大厅又不算邀标"的幽灵单）；公开招标携带 `invitedMerchantIds` → 400（不再静默忽略）；解析收件人与"无人可邀/勾选无效"校验提到额度评估（NEED_POINTS 确认框）之前；商户名义发布排除发布商户自己 |
| `GET /api/sourcing/opportunities`、`/matched` | 本商户"可匹配型号"改走新增 `IMerchantBearingRepository.GetMatchableByMerchantAsync`（审核通过 且 出售中/补货中 且 未停用）——与原 `SourcingMatcher` 口径一致（原用只在售口径，补货中商家被通知却在聚合面看不到）；`/matched` 邀标单**优先参与截断**（`OrderByDescending(IsPrivate)` 后按时间，原按时间取 50 可能把较早邀标单挤出） |
| `DELETE /api/sourcing/demands/{id:guid}/respond` | 撤回应答同步递减 `SourcingDemand.ResponseCount`（原残留计数会让该单在 `/matched`、`/opportunities` 的"尚无人应答"判定中永久消失） |
| `GET /api/admin/sourcing/demands`、`/demands/{id:guid}` | 治理输出补 `publisherMerchantId`/`publisherMerchantName`/`publisherType`（Admin 列表发布人列标注商户名义，详情加行） |

> 匹配口径（`SourcingMatcher`）：改为复用实体 `MerchantBearing.IsPubliclyVisible`（审核通过 且 出售中/补货中 且 未停用）与商户 `Status = Active`（原只判 `IsVerified`），并支持排除指定商户。
> 通知（详见《通知消息体系设计 v1.2.0》）：**邀标单豁免** `Sourcing.NotifyMaxPerMerchantPerDay` 日限频——受邀是发布人点名勾选、受众可控，"受邀必达"优先；公开招标保持限频以抑制刷屏。
> 配套文档：《搜索寻货与数据可信联动设计 v1.3.0》《通知消息体系设计 v1.2.0》《移动端登录与商户入驻设计 v1.7.39》《BFF API 端点说明 v1.8.3》。
