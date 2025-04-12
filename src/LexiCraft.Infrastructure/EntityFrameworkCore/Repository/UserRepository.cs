using LexiCraft.Domain.Repository;
using LexiCraft.Domain.Users;

namespace LexiCraft.Infrastructure.EntityFrameworkCore.Repository;

public class UserRepository(LexiCraftDbContext dbContext) 
    : Repository<LexiCraftDbContext, User>(dbContext), IUserRepository<LexiCraftDbContext>
{
    
}