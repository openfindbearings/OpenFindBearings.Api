# API 端点说明文档

**版本：** v1.34.0
**日期：** 2026-09-24
**状态：** 与代码同步

---

## 变更记录

| 版本 | 日期 | 变更说明 |
|------|------|----------|
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
| v1.19.0 | 2026-09-16 | 媒体服务改独立 nginx 直出（应用不再逐字节代理图片）：① 新增 `deploy/k3s/media-server.yml`（nginx 只读挂 sync hostPath `/opt/openfindbearings/sync/images` + api hostPath `/opt/openfindbearings/api/uploads`，对外 `bff.515813.xyz/media/{images,uploads}/**`）+ API `deploy.yml` uploads hostPath 权限从 700 改 755（媒体服务以不同 uid 只读挂载需 other 位读穿越）；② `Mobile.*` 新增第六键 `MediaBaseUrl`（`https://bff.515813.xyz/media`），`GET /api/mobile/config` 响应新增 `mediaBaseUrl` 字段，前端 `getMediaBase()` 拼接；③ URL 契约治本：库内只存相对媒体键，`UserEndpoints.UploadAvatar`/`MerchantEndpoints.UploadLogo` 保持返回相对 `/uploads/...`（前端与 BFF 都不再拼绝对），`Identity/UpdateProfileRequest.PictureUrl` `[Url]` 改相对/绝对双允许正则（此前 `[Url]` 强制绝对，是历史绝对地址入库根因）；④ 幂等一次性 SQL 归一历史绝对 URL：`deploy/k3s/media-url-migration.sql`；⑤ BFF `MediaEndpoints.cs` + `ApiClient.GetRawAsync` + `MeEndpoints.PublicUrl` 删除（BFF 端点 -1） |
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

OpenFindBearings.Api（以下简称 API）共注册 **170** 个端点，按职责划分为 8 组。另提供内部配置端点 `/api/config/reliability` 供 Sync 拉取可信度阈值（不计入 8 组统计）。

| 组 | 路由前缀 | 端点数量 | 认证策略 |
|---|---------|---------|---------|
| 公共 | `/api` | 13 | 全部匿名 |
| 移动端 | `/api/mobile` | 2 | 全部匿名 |
| 用户 | `/api/me` | 24 | Bearer（Authenticated 策略） |
| 站内信 | `/api/notifications` | 6 | Bearer（Authenticated 策略） |
| 入驻 | `/api/merchant` | 10 | Bearer（任意已登录用户） |
| 商家 | `/api/merchant` | 19 | Bearer（Merchant 策略，分组限流） |
| 后台管理 | `/api/admin` | 61 | Bearer（Admin 策略）+ RequirePermission |
| 同步 | `/api/sync` | 6 | Bearer（SyncClient 策略） |

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
> - 服务端版本配置读取 `Mobile.{platform}.Version`，回退 `Mobile.AppVersion`（同理 MinVersion / ForceUpdate / DownloadUrl / UpdateMessage 五键族）。`Mobile.DownloadUrl` 是 APK 下载**目录**（以 `/` 结尾，K3s 自建静态服务 `https://bff.515813.xyz/dl/`），客户端按 `app-v<版本>-<ABI>.apk` 拼文件名；GitHub Release 为拉取源（服务器 CronJob 同步），不直接面向用户分发。
> - 五键由 SeedData 播种，存量库经 `EnsureConfigKeysAsync` 启动时幂等补全（无 EF 迁移）。发布新版流程（v1.18.0 起为集群拉取式）：Publish Release（规范 tag `vX.Y.Z[-rc.N]`）→ Taro CI 构建 APK 传 Release（跨境推包/写库步骤已删除）→ K3s CronJob `openfindbearings-apk-sync` 每晚 4 点（北京时间）拉最新规范版本分包（sha256 校验、`-c` 断点续传 + shell 重试循环抗链路抖动），两包齐备才改写 `Mobile.AppVersion`、写固定模板 `Mobile.UpdateMessage`，并清理历史版分包只留当前版；拉不完不宣告、下轮续传。Admin 系统配置页仅作手动兜底。
> - v1.19.0 媒体：图片不再由 BFF 逐字节代理，改独立 nginx 媒体服务在 `bff.515813.xyz/media/**` 直出。`GET /api/mobile/config` 响应含 `mediaBaseUrl`（读 `Mobile.MediaBaseUrl`，如 `https://bff.515813.xyz/media`，末尾无斜杠）。**媒体 URL 契约：库内一律存相对键**（`/images/...` 爬虫、`/uploads/...` 用户上传、`/avatars/presets/...` 预置），host 由客户端 `getMediaBase()` 拼（可被 `mediaBaseUrl` 运行时覆盖）；上传端点（头像/商户 Logo/执照）返回相对键。换域名/切对象存储只需改 `Mobile.MediaBaseUrl`，库内键不变。预置头像打包进 Taro 客户端本地，不经服务器。

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
| POST | `/api/merchant/{merchantId:guid}/close` | **v1.30.0** 商户自助关店：任一在职管理员发起；claim/提名已有→解除归属回公海（商品/证照/成员/邀请/纠错全清，Contact 隐私止血，DataSource 回 Crawler+Status→Pending，唤醒 Sync staging 刷新）；self/提名新建→直接删除。幂等可重入，409/400 透传守卫原因 |
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

## 7. 商家端点 `/api/merchant`（23 个）

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
| POST | `/api/merchant/inventory/import` | 可操作 | 拒绝 |

> v1.14.0 说明：`POST /api/merchant/inventory/import` 为新增 Excel 批量导入端点（仅商户管理员，`.DisableAntiforgery()`）。API 用 sync-client 客户端凭据换 `api:sync` token 后转发到 Sync `POST /api/inventory/import`，写库 `DataSourceType=Manual`（不被爬虫覆盖）。配置见 appsettings.json `Sync` 节。

---

## 8. 后台管理端点 `/api/admin`（66 个）

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
| GET | `/api/config/reliability` | 需认证（SyncClient/Admin 等任意有效令牌），无需 system.view；返回可信度三阈值，供 Sync 运行时拉取 |

---

## 9. 同步端点 `/api/sync`（6 个）

组授权：`RequireAuthorization("SyncClient")`。用于 FindBearings.Sync 项目的 L 阶段数据加载。

| 方法 | 路由 | 说明 | WithName |
|------|------|------|----------|
| POST | `/api/sync/brands/batch` | 批量同步品牌 | `SyncBrands` |
| POST | `/api/sync/bearingtypes/batch` | 批量同步轴承类型 | `SyncBearingTypes` |
| POST | `/api/sync/bearings/batch` | 批量同步轴承 | `BatchCreateBearings` |
| POST | `/api/sync/merchants/batch` | 批量同步商家 | `BatchCreateMerchants` |
| POST | `/api/sync/merchantbearings/batch` | 批量同步关联 | `BatchCreateMerchantBearings` |
| POST | `/api/sync/interchanges/batch` | 批量同步替代品 | `BatchCreateInterchanges` |

---

## 10. 寻货端点 `/api/sourcing`（9 个）

| 方法 | 路径 | 说明 | 鉴权 |
|---|---|---|---|
| GET | /demands | 进行中寻货 feed（keyword 型号搜索、onlyOpen 过滤、分页） | 匿名 |
| GET | /demands/{id:guid} | 详情（登录带商户时返回 myResponse；选定后返回解锁联系方式） | 匿名 |
| POST | /demands | 发布寻货（免费 3 条/日，超限 NEED_POINTS 协议 + usePoints 重提交，硬上限 10） | 登录 |
| POST | /demands/{id:guid}/respond | 商户应答（一商户一需求单条可更新；免费 20/日超限花积分，硬上限 50） | 登录+商户 |
| POST | /demands/{id:guid}/select | 选定应答（关闭需求、其余置未选中、双方解锁联系方式） | 登录+发布人 |
| POST | /demands/{id:guid}/cancel | 取消寻货（软关闭，通知全体应答者） | 登录+发布人 |
| GET | /my/demands | 我发布的寻货 | 登录 |
| GET | /my/responses | 当前商户的应答记录 | 登录+商户 |
| GET | /quota | 额度聚合（发布/应答免费额度、今日已用、硬上限、积分单价、余额——额度条数据源，v1.35.0） | 登录 |
| GET | /my-offering | 我的在售同款（当前商户对该型号的在售/补货中条目，应答表单预填数据源，v1.37.0） | 登录+商户 |
| GET | /opportunities | 需求信号（商户在售型号中被寻货且未应答的聚合清单，反向导购横幅数据源，v1.37.0） | 登录+商户 |

## 11. 积分端点 `/api/points`（4 个）

| 方法 | 路径 | 说明 | 鉴权 |
|---|---|---|---|
| GET | /account | 余额/累计/连签天数 | 登录 |
| POST | /checkin | 每日签到（阶梯 [2,3,4,5,5]） | 登录 |
| GET | /transactions | 收支明细分页 | 登录 |
| GET | /tasks | 赚分任务清单（启用规则+完成态：daily 看今日/once 看历史；含今日次数与上限，v1.34.0） | 登录 |

## 12. 端口配置

| 环境 | HTTP | HTTPS |
|------|------|-------|
| 开发 | 5183 | 7183 |
| 生产 | 8080 | — |

---

## 13. 健康检查端点

| 端点 | 说明 |
|------|------|
| `/health` | 详细 JSON 健康检查 |
| `/healthz` | 纯文本健康检查 |
| `/health/live` | K8s 存活探针 |
| `/health/ready` | K8s 就绪探针 |
