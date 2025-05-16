using Gnarly.Data;
using LexiCraft.Domain.Repository;
using LexiCraft.Domain.Users;

namespace LexiCraft.Infrastructure.EntityFrameworkCore.Repository;

[Registration(typeof(IUserRepository<LexiCraftDbContext>))]
public class UserRepository(LexiCraftDbContext dbContext) 
    : Repository<LexiCraftDbContext, User>(dbContext), IUserRepository<LexiCraftDbContext>, IScopeDependency
{
    
}