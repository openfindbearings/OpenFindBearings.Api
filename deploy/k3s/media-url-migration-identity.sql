-- 媒体 URL 归一（Identity 库专用）
-- 头像此前受 UpdateProfileRequest.PictureUrl 上的 [Url] 校验强制绝对入库，是历史绝对地址的根因。
-- 校验已改为相对/绝对双允许正则（v2.4.0）；本脚本一次性把已入库的媒体树绝对地址洗成相对键。
-- 幂等：已是相对键的行 WHERE 不匹配跳过；指向外部站（如 example.com）不匹配本媒体树前缀保持原样
-- 归一规则（两步嵌套）：
--   1) 剥离可选 origin + /mobile/media 代理前缀：   ^(https?://[^/]+)?/mobile/media(?=/) -> ''
--   2) 剥离指向本媒体树的 origin：                 ^https?://[^/]+(?=/uploads/|/images/|/avatars/) -> ''

UPDATE "Users"
   SET "PictureUrl" = regexp_replace(
         regexp_replace("PictureUrl", '^(https?://[^/]+)?/mobile/media(?=/)', ''),
         '^https?://[^/]+(?=/uploads/|/images/|/avatars/)', '')
 WHERE "PictureUrl" ~ '^(https?://|/mobile/media/)';
