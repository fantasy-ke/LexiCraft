using BuildingBlocks.EntityFrameworkCore.Repositories;
using Fantasy.Services.Identity.Identity.Models;
using Fantasy.Services.Identity.Shared.Contracts;
using Fantasy.Services.Identity.Shared.Data;

namespace Fantasy.Services.Identity.Identity.Data.Repositories;

public class UserRepository(IdentityDbContext dbContext)
    : Repository<IdentityDbContext, User>(dbContext), IUserRepository;