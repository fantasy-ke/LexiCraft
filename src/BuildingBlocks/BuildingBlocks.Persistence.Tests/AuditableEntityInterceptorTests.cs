using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Domain.Internal;
using BuildingBlocks.EntityFrameworkCore.Repositories;
using BuildingBlocks.EntityFrameworkCore.Extensions;
using BuildingBlocks.EntityFrameworkCore.Interceptors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Persistence.Tests;

public class AuditableEntityInterceptorTests
{
    [Fact]
    public async Task SaveChanges_sets_utc_audit_values_and_soft_deletes_without_removing_row()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var userId = Guid.NewGuid();
        var interceptor = new AuditableEntityInterceptor(new TestUserContext(userId, "tester"));
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var entity = new TestAuditEntity { Name = "audit" };
        context.Entities.Add(entity);
        await context.SaveChangesAsync();

        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal(DateTimeKind.Utc, entity.CreateAt.Kind);
        Assert.Equal(userId, entity.CreateById);
        Assert.Equal("tester", entity.CreateByName);
        Assert.False(entity.IsDeleted);

        context.Entities.Remove(entity);
        await context.SaveChangesAsync();
        Assert.NotNull(entity.DeleteAt);
        Assert.Equal(DateTimeKind.Utc, entity.DeleteAt!.Value.Kind);
        var deleteAt = entity.DeleteAt.Value;
        context.ChangeTracker.Clear();

        var stored = await context.Entities.SingleAsync();
        Assert.True(stored.IsDeleted);
        Assert.NotNull(stored.DeleteAt);
        Assert.Equal(deleteAt, stored.DeleteAt.Value);
        Assert.Equal(userId, stored.DeleteById);
        Assert.Equal("tester", stored.DeleteByName);
    }

    [Fact]
    public async Task Soft_delete_of_detached_entity_only_updates_delete_fields()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var userId = Guid.NewGuid();
        var interceptor = new AuditableEntityInterceptor(new TestUserContext(userId, "tester"));
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options;

        Guid entityId;
        await using (var createContext = new TestDbContext(options))
        {
            await createContext.Database.EnsureCreatedAsync();
            var entity = new TestAuditEntity { Name = "preserve-me" };
            createContext.Entities.Add(entity);
            await createContext.SaveChangesAsync();
            entityId = entity.Id;
        }

        await using (var deleteContext = new TestDbContext(options))
        {
            deleteContext.Entities.Remove(new TestAuditEntity { Id = entityId });
            await deleteContext.SaveChangesAsync();
        }

        await using var verifyContext = new TestDbContext(options);
        var stored = await verifyContext.Entities.SingleAsync();
        Assert.Equal("preserve-me", stored.Name);
        Assert.True(stored.IsDeleted);
        Assert.Equal(userId, stored.DeleteById);
        Assert.Equal("tester", stored.DeleteByName);
    }

    [Theory]
    [InlineData(0, 10, "pageIndex")]
    [InlineData(1, 0, "pageSize")]
    public async Task QueryRepository_rejects_invalid_pagination(
        int pageIndex,
        int pageSize,
        string parameterName)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var context = new TestDbContext(options);
        var repository = new QueryRepository<TestDbContext, TestAuditEntity>(context);

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            repository.GetPageListAsync(_ => true, pageIndex, pageSize));

        Assert.Equal(parameterName, exception.ParamName);
    }

    [Fact]
    public async Task Queryable_extension_honors_cancellation()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            context.Entities.GetPageListAsync(
                _ => true,
                1,
                10,
                cancellationToken: cancellationTokenSource.Token));
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestAuditEntity> Entities => Set<TestAuditEntity>();
    }

    private sealed class TestAuditEntity : AuditAggregateRoot<Guid>
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestUserContext : IUserContext
    {
        public TestUserContext(Guid userId, string userName)
        {
            UserId = userId;
            UserName = userName;
        }

        public Guid UserId { get; }
        public string UserName { get; }
        public string UserAccount => UserName;
        public bool IsAuthenticated => true;
        public string[] Roles => [];
    }
}
