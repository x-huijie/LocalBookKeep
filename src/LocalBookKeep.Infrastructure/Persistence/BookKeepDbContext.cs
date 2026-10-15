using LocalBookKeep.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocalBookKeep.Infrastructure.Persistence;

/// <summary>
/// 记账应用数据上下文。
/// </summary>
/// <remarks>
/// 通过 <c>DatabaseOptions.Provider</c> 切换数据库提供程序，
/// 当前默认 SQLite（本地单文件），后续可无缝切换到 MySQL。
/// </remarks>
public sealed class BookKeepDbContext : DbContext
{
    /// <summary>构造数据上下文。</summary>
    public BookKeepDbContext(DbContextOptions<BookKeepDbContext> options) : base(options)
    {
    }

    /// <summary>账单表。</summary>
    public DbSet<Transaction> Transactions => Set<Transaction>();

    /// <summary>分类表。</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>账户表。</summary>
    public DbSet<Account> Accounts => Set<Account>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(builder =>
        {
            builder.ToTable("Accounts");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(20);
            builder.Property(x => x.InitialBalance).HasPrecision(18, 2);
            builder.Property(x => x.IsBuiltIn).HasDefaultValue(false);
            builder.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Category>(builder =>
        {
            builder.ToTable("Categories");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(20);
            builder.Property(x => x.IsBuiltIn).HasDefaultValue(false);
            builder.Property(x => x.Type).HasConversion<int>();
            // 同一收支类型下分类名唯一（支出「餐饮」和收入「餐饮」可以并存）
            builder.HasIndex(x => new { x.Type, x.Name }).IsUnique();
        });

        modelBuilder.Entity<Transaction>(builder =>
        {
            builder.ToTable("Transactions");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.Type).HasConversion<int>();
            builder.Property(x => x.Remark).HasMaxLength(200);
            builder.HasIndex(x => x.OccurredOn);

            builder.HasOne(x => x.Category)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Account)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
