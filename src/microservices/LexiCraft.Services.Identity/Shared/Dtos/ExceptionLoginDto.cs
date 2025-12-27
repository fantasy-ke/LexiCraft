namespace LexiCraft.Services.Identity.Shared.Dtos;

public record ExceptionLoginDto(string Message, string UserAccount, string LoginType);