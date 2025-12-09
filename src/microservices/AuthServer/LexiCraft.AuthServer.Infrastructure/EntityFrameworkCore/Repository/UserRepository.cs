using BuildingBlocks.EntityFrameworkCore;
using Gnarly.Data;
using LexiCraft.AuthServer.Domain.Repository;
using LexiCraft.AuthServer.Domain.Users;

namespace LexiCraft.AuthServer.Infrastructure.EntityFrameworkCore.Repository;

[Registration(typeof(IUserRepository<LexiCraftDbContext>))]
public class UserRepository(LexiCraftDbContext dbContext) 
    : Repository<LexiCraftDbContext, User>(dbContext), IUserRepository<LexiCraftDbContext>, IScopeDependency;