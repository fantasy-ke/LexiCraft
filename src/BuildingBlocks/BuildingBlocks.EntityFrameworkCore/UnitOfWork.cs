﻿using BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.EntityFrameworkCore;

public class UnitOfWork<TDbContext>(TDbContext dbContext) : IUnitOfWork where TDbContext : DbContext
{
    public async Task BeginTransactionAsync()
    {
        await dbContext.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        await dbContext.Database.CommitTransactionAsync();
    }

    public async Task RollbackTransactionAsync()
    {
        await dbContext.Database.RollbackTransactionAsync();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await dbContext.SaveChangesAsync();
    }
}