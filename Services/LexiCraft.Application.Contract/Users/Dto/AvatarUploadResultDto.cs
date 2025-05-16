namespace LexiCraft.Application.Contract.Users.Dto;

public class AvatarUploadResultDto
{
    // AvatarUrl = avatarUrl,
    // FileId = fileInfo.Id
    public string? AvatarUrl { get; set; }
    
    public Guid? FileId { get; set; }
    
}