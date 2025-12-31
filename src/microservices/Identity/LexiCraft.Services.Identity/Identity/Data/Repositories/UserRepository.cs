using BuildingBlocks.EntityFrameworkCore;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Shared.Contracts;
using LexiCraft.Services.Identity.Shared.Data;

namespace LexiCraft.Services.Identity.Identity.Data.Repositories;

public class UserRepository(IdentityDbContext dbContext) 
    : Repository<IdentityDbContext, User>(dbContext), IUserRepository;