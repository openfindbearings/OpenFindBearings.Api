-- 媒体 URL 归一（业务 API 库专用）
-- 把历史上以绝对地址 / BFF 代理前缀入库的媒体值洗成相对媒体键（/uploads/...、/images/...、/avatars/...）
-- 幂等：已是相对键的行不匹配 WHERE 不动；指向外部站（如 example.com）的绝对 URL 不匹配本媒体树前缀，保持原样
-- 归一规则（两步嵌套）：
--   1) 剥离可选 origin + /mobile/media 代理前缀：   ^(https?://[^/]+)?/mobile/media(?=/) -> ''
--   2) 剥离指向本媒体树的 origin：                 ^https?://[^/]+(?=/uploads/|/images/|/avatars/) -> ''

-- 用户头像
UPDATE "Users"
   SET "Avatar" = regexp_replace(
         regexp_replace("Avatar", '^(https?://[^/]+)?/mobile/media(?=/)', ''),
         '^https?://[^/]+(?=/uploads/|/images/|/avatars/)', '')
 WHERE "Avatar" ~ '^(https?://|/mobile/media/)';

-- 商户 Logo
UPDATE "Merchants"
   SET "LogoUrl" = regexp_replace(
         regexp_replace("LogoUrl", '^(https?://[^/]+)?/mobile/media(?=/)', ''),
         '^https?://[^/]+(?=/uploads/|/images/|/avatars/)', '')
 WHERE "LogoUrl" ~ '^(https?://|/mobile/media/)';

-- 品牌 Logo（表存在性守卫：无该表时 NOTICE 跳过，不阻断脚本）
DO $$
DECLARE
  v_exists boolean;
BEGIN
  SELECT EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = current_schema() AND table_name = 'Brands'
  ) INTO v_exists;
  IF v_exists THEN
    EXECUTE $sql$
      UPDATE "Brands"
         SET "LogoUrl" = regexp_replace(
               regexp_replace("LogoUrl", '^(https?://[^/]+)?/mobile/media(?=/)', ''),
               '^https?://[^/]+(?=/uploads/|/images/|/avatars/)', '')
       WHERE "LogoUrl" ~ '^(https?://|/mobile/media/)'
    $sql$;
    RAISE NOTICE 'Brands 表存在，已处理';
  ELSE
    RAISE NOTICE 'Brands 表不存在，跳过';
  END IF;
END$$;
