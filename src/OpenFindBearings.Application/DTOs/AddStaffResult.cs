namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 添加员工结果
    /// </summary>
    public class AddStaffResult
    {
        public bool Success { get; set; }
        public bool IsInvitationSent { get; set; }
        public Guid? UserId { get; set; }
        public Guid? InvitationId { get; set; }
        public string? Message { get; set; }
        public bool EmailSent { get; set; }
        public bool SmsSent { get; set; }

        /// <summary>
        /// 已是在职成员（幂等提示，v2.9.0 邀请确认制防重复邀请）
        /// </summary>
        public static AddStaffResult AlreadyMember(string merchantName) =>
            new() { Success = true, IsInvitationSent = false, Message = $"该用户已是「{merchantName}」的在职成员" };

        public static AddStaffResult InvitationSent(Guid invitationId, bool emailSent, bool smsSent) =>
            new()
            {
                Success = true,
                IsInvitationSent = true,
                InvitationId = invitationId,
                EmailSent = emailSent,
                SmsSent = smsSent,
                Message = "邀请已发送，请等待用户完成注册"
            };
    }
}
