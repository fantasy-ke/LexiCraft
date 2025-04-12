using LexiCraft.Domain.Users;

namespace LexiCraft.Domain.Repository;

public interface IUserRepository<TDbContext> : IRepository<TDbContext, User>
{
    
}