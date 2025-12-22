namespace LexiCraft.Services.Identity.Users.Dtos;

/// <summary>
/// 上传头像结果
/// </summary>
public sealed class AvatarUploadResultDto
{
    /// <summary>
    /// 头像URL
    /// </summary>
    public string AvatarUrl { get; set; } = default!;

    /// <summary>
    /// 文件ID
    /// </summary>
    public Guid? FileId { get; set; }
}