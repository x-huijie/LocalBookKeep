using System.Text;
using LocalBookKeep.Application.Abstractions;
using LocalBookKeep.Application.Dtos;
using LocalBookKeep.Domain.Enums;

namespace LocalBookKeep.Tests;

/// <summary>
/// CSV 导出与数据库备份/恢复的验证。
/// </summary>
public sealed class ExportAndBackupTests
{
    [Fact]
    public async Task 导出CSV包含表头与全部账单且带BOM()
    {
        using var db = new TestDatabase();
        var cash = (await db.Accounts.GetAllAsync()).Single(x => x.Name == "现金");
        var food = (await db.Categories.GetByTypeAsync(TransactionType.Expense)).Single(x => x.Name == "餐饮");

        await db.Transactions.CreateAsync(new TransactionInput
        {
            Type = TransactionType.Expense,
            Amount = 25.5m,
            CategoryId = food.Id,
            AccountId = cash.Id,
            OccurredOn = new DateOnly(2026, 3, 5),
            Remark = "晚饭"
        });

        var exporter = db.GetRequiredService<ITransactionCsvExporter>();
        var filePath = Path.Combine(db.Directory, "export.csv");

        var count = await exporter.ExportAsync(await db.Transactions.GetAllAsync(), filePath);

        Assert.Equal(1, count);
        Assert.True(File.Exists(filePath));

        var bytes = await File.ReadAllBytesAsync(filePath);
        Assert.True(bytes.Length > 3);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);

        var lines = await File.ReadAllLinesAsync(filePath);
        Assert.Equal(2, lines.Length);
        Assert.Equal("日期,类型,金额,分类,账户,备注", lines[0]);
        Assert.Contains("2026-03-05", lines[1]);
        Assert.Contains("支出", lines[1]);
        Assert.Contains("25.5", lines[1]);
        Assert.Contains("餐饮", lines[1]);
        Assert.Contains("现金", lines[1]);
        Assert.Contains("晚饭", lines[1]);
    }

    [Fact]
    public async Task 备份文件可以完整恢复数据()
    {
        using var db = new TestDatabase();
        var cash = (await db.Accounts.GetAllAsync()).Single(x => x.Name == "现金");
        var food = (await db.Categories.GetByTypeAsync(TransactionType.Expense)).Single(x => x.Name == "餐饮");

        await db.Transactions.CreateAsync(new TransactionInput
        {
            Type = TransactionType.Expense,
            Amount = 100m,
            CategoryId = food.Id,
            AccountId = cash.Id,
            OccurredOn = new DateOnly(2026, 3, 5),
            Remark = "备份前的数据"
        });

        var backupService = db.GetRequiredService<IDatabaseBackupService>();
        var backupDirectory = Path.Combine(db.Directory, "backup");
        var backupPath = await backupService.BackupAsync(backupDirectory);

        Assert.True(File.Exists(backupPath));
        Assert.Contains("LocalBookKeep_backup_", Path.GetFileName(backupPath));

        // 备份之后再写一条，恢复后应该消失
        await db.Transactions.CreateAsync(new TransactionInput
        {
            Type = TransactionType.Expense,
            Amount = 200m,
            CategoryId = food.Id,
            AccountId = cash.Id,
            OccurredOn = new DateOnly(2026, 3, 6),
            Remark = "备份后的数据"
        });

        Assert.Equal(2, (await db.Transactions.GetAllAsync()).Count);

        await backupService.RestoreAsync(backupPath);

        var restored = await db.Transactions.GetAllAsync();
        Assert.Single(restored);
        Assert.Equal("备份前的数据", restored[0].Remark);
        Assert.Equal(100m, restored[0].Amount);

        var account = (await db.Accounts.GetAllAsync()).Single(x => x.Id == cash.Id);
        Assert.Equal(-100m, account.CurrentBalance);
    }

    [Fact]
    public async Task 恢复非SQLite文件会被拒绝()
    {
        using var db = new TestDatabase();
        var backupService = db.GetRequiredService<IDatabaseBackupService>();
        var fake = Path.Combine(db.Directory, "not-a-database.db");
        await File.WriteAllTextAsync(fake, "这不是一个数据库文件", Encoding.UTF8);

        await Assert.ThrowsAsync<ArgumentException>(() => backupService.RestoreAsync(fake));
    }
}
