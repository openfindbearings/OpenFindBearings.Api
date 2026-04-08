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

        public static AddStaffResult Linked(Guid userId) =>
            new() { Success = true, IsInvitationSent = false, UserId = userId, Message = "员工已添加" };

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
