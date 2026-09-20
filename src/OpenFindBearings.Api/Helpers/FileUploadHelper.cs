namespace OpenFindBearings.Api.Helpers
{
    /// <summary>
    /// 文件上传公共辅助：统一扩展名解析口径。
    /// 背景：部分 RN 客户端 multipart 文件名的扩展名缺失（Taro 3.6 RN uploadFile 硬编码 name:'file'），
    /// 仅按 FileName 取扩展名会把合法图片判成"格式不支持"，故缺扩展名时回退按 Content-Type 推断。
    /// </summary>
    public static class FileUploadHelper
    {
        /// <summary>
        /// 取上传文件的小写扩展名（含点，如 ".jpg"）；文件名为空扩展名时按 MIME 推断，推不出返回空串。
        /// </summary>
        public static string GetSafeExtension(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!string.IsNullOrEmpty(ext))
            {
                return ext;
            }

            // 改动说明：无扩展名回退 MIME 推断（与端点白名单口径一致：图片 + PDF + Excel）
            var mime = file.ContentType?.ToLowerInvariant();
            return mime switch
            {
                "image/jpeg" or "image/jpg" or "image/pjpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "application/pdf" => ".pdf",
                "application/vnd.ms-excel" => ".xls",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
                _ => string.Empty
            };
        }
    }
}
