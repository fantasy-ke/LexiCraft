using BuildingBlocks.EntityFrameworkCore;
using LexiCraft.AuthServer.Domain.Repository;
using LexiCraft.AuthServer.Domain.Users;
using ZAnalyzers.Core;

namespace LexiCraft.AuthServer.Infrastructure.EntityFrameworkCore.Repository;

[Registration(typeof(IUserRepository))]
public class UserRepository(LexiCraftDbContext dbContext) 
    : Repository<LexiCraftDbContext, User>(dbContext), IUserRepository, IScopeDependency;