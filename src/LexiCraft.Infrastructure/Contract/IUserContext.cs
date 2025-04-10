using System.Security.Claims;

namespace LexiCraft.Infrastructure.Contract;

public interface IUserContext
{
    public Guid UserId { get; }

    public string UserName { get; }
    

    bool IsAuthenticated { get; }
    
    string[] Roles { get; }
    
}