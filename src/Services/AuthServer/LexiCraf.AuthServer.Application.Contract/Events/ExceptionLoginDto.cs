
namespace LexiCraf.AuthServer.Application.Contract.Events;

public record ExceptionLoginDto(string Message,string UserAccount,string LoginType);