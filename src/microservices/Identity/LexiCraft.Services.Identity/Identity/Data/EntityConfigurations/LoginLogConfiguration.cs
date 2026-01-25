using LexiCraft.Services.Identity.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexiCraft.Services.Identity.Identity.Data.EntityConfigurations;

public class LoginLogConfiguration : IEntityTypeConfiguration<LoginLog>
{
    public void Configure(EntityTypeBuilder<LoginLog> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.UserId);
    }
}