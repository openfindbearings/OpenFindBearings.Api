-- 媒体 URL 归一：把历史上以绝对地址 / BFF 代理前缀入库的媒体值，洗成相对媒体键
-- （/uploads/...、/images/...、/avatars/...），配合"相对键入库 + 前端拼媒体源"的新契约。
-- 幂等：已是相对键的行不匹配 WHERE，不改动；指向外部站的绝对 URL（如 example.com）路径不匹配
-- 本媒体树前缀，regexp 不剥离，保持原样。
--
-- 归一规则（两步嵌套）：
--   1) 去掉可选 origin + /mobile/media 代理前缀：   ^(https?://[^/]+)?/mobile/media(?=/) -> ''
--   2) 去掉指向本媒体树的 origin：                 ^https?://[^/]+(?=/uploads/|/images/|/avatars/) -> ''
--
-- 执行前务必备份；分别在【业务 API 库】与【Identity 库】上跑对应语句。

-- ========== 业务 API 库（OpenFindBearings.Api 连接串指向的库）==========

UPDATE "Users"
   SET "Avatar" = regexp_replace(
         regexp_replace("Avatar", '^(https?://[^/]+)?/mobile/media(?=/)', ''),
         '^https?://[^/]+(?=/uploads/|/images/|/avatars/)', '')
 WHERE "Avatar" ~ '^(https?://|/mobile/media/)';

UPDATE "Merchants"
   SET "LogoUrl" = regexp_replace(
         regexp_replace("LogoUrl", '^(https?://[^/]+)?/mobile/media(?=/)', ''),
         '^https?://[^/]+(?=/uploads/|/images/|/avatars/)', '')
 WHERE "LogoUrl" ~ '^(https?://|/mobile/media/)';

UPDATE "Brands"
   SET "LogoUrl" = regexp_replace(
         regexp_replace("LogoUrl", '^(https?://[^/]+)?/mobile/media(?=/)', ''),
         '^https?://[^/]+(?=/uploads/|/images/|/avatars/)', '')
 WHERE "LogoUrl" ~ '^(https?://|/mobile/media/)';

-- ========== Identity 库（认证中心连接串指向的库）==========
-- 头像此前受 PictureUrl [Url] 强制绝对，一并归一（外部 example.com 种子不受影响）

UPDATE "Users"
   SET "PictureUrl" = regexp_replace(
         regexp_replace("PictureUrl", '^(https?://[^/]+)?/mobile/media(?=/)', ''),
         '^https?://[^/]+(?=/uploads/|/images/|/avatars/)', '')
 WHERE "PictureUrl" ~ '^(https?://|/mobile/media/)';
