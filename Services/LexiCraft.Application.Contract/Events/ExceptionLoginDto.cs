
namespace LexiCraft.Application.Contract.Events;

public record ExceptionLoginDto(string Message,string UserAccount,string LoginType);