﻿using LexiCraft.Files.Grpc.Model;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Files.Grpc.Data;

public class FilesDbContext(DbContextOptions<FilesDbContext> options) : DbContext(options)
{
    public DbSet<FileInfos> FileInfos { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // modelBuilder.Entity<Coupon>().HasData(
        //     new Coupon { Id = 1, ProductName = "IPhone X", Description = "IPhone Discount", Amount = 150 },
        //     new Coupon { Id = 2, ProductName = "Samsung 10", Description = "Samsung Discount", Amount = 100 }
        // );
    }
}