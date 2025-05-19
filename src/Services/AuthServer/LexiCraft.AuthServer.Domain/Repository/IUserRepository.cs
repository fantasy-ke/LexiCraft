using BuildingBlocks.Domain;
using LexiCraft.AuthServer.Domain.Users;

namespace LexiCraft.AuthServer.Domain.Repository;

public interface IUserRepository<TDbContext> : IRepository<TDbContext, User>
{
    
}