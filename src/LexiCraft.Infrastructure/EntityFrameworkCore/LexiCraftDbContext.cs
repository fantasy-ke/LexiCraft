using IdGen;
using LexiCraft.Domain.Internal;
using LexiCraft.Domain.Users;
using LexiCraft.Infrastructure.Contract;
using LexiCraft.Infrastructure.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace LexiCraft.Infrastructure.EntityFrameworkCore;

public class LexiCraftDbContext(DbContextOptions options,IServiceProvider? serviceProvider = null): DbContext(options)
{
    public DbSet<User> Users { get; set; }
    
    public DbSet<UserSetting> UserSettings { get; set; }
    
    public DbSet<UserOAuth> UserOAuths { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigureAuth();
    }
    
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // 忽略未应用的模型更改警告，这个是.net9出现的新警告，不忽略会影响迁移记录应用
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
    }


    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        BeforeSaveChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = new())
    {
        BeforeSaveChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void BeforeSaveChanges()
    {
        var entries = ChangeTracker.Entries()
            .Where(x => x.State is EntityState.Added or EntityState.Modified);
        var userContext = serviceProvider.GetService<IUserContext>();
        var idGenerator = serviceProvider.GetService<IdGenerator>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                switch (entry.Entity)
                {
                    case IEntity<Guid> guidId:
                        guidId.Id = Guid.NewGuid();
                        break;
                    case IEntity<long> longId:
                        longId.Id = idGenerator?.CreateId() ?? 0;
                        break;
                }

                if (entry.Entity is ICreatable entity)
                {
                    entity.CreateAt = DateTime.Now;
                }

                switch (entry.Entity)
                {
                    case ICreatable<Guid?> creatable:
                        creatable.CreateById = userContext?.UserId;
                        creatable.CreateByName = userContext?.UserName;
                        break;
                    case ICreatable<Guid> creatableValue:
                        creatableValue.CreateById = userContext?.UserId ?? Guid.Empty;
                        creatableValue.CreateByName = userContext?.UserName;
                        break;
                }
            }
            else
            {
                if (entry.Entity is IUpdatable entity)
                {
                    entity.UpdateAt = DateTime.Now;
                }

                switch (entry.Entity)
                {
                    case IUpdatable<Guid?> updatable:
                        updatable.UpdateById = userContext?.UserId;
                        updatable.UpdateByName = userContext?.UserName;
                        break;
                    case IUpdatable<Guid> updatableValue:
                        updatableValue.UpdateById = userContext?.UserId ?? Guid.Empty;
                        updatableValue.UpdateByName = userContext?.UserName;
                        break;
                }
            }
        }
    }
}