using LocalBookKeep.Application.DependencyInjection;
using LocalBookKeep.Application.Services;
using LocalBookKeep.Infrastructure.DependencyInjection;
using LocalBookKeep.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace LocalBookKeep.Tests;

/// <summary>
/// 每个测试用例使用一个独立的临时 SQLite 文件（真实走 EF Core + DI 组合根）。
/// </summary>
internal sealed class TestDatabase : IDisposable
{
    private readonly ServiceProvider _provider;

    public TestDatabase()
    {
        Directory = Path.Combine(Path.GetTempPath(), "LocalBookKeepTests", Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(Directory);
        DatabasePath = Path.Combine(Directory, "bookkeep.db");

        var services = new ServiceCollection();
        services.AddBookKeepApplication();
        services.AddBookKeepInfrastructure(DatabaseOptions.Sqlite(DatabasePath));
        _provider = services.BuildServiceProvider();

        _provider.GetRequiredService<DatabaseInitializer>().InitializeAsync().GetAwaiter().GetResult();
    }

    public string Directory { get; }

    public string DatabasePath { get; }

    public TransactionService Transactions => _provider.GetRequiredService<TransactionService>();

    public CategoryService Categories => _provider.GetRequiredService<CategoryService>();

    public AccountService Accounts => _provider.GetRequiredService<AccountService>();

    public SummaryService Summary => _provider.GetRequiredService<SummaryService>();

    public T GetRequiredService<T>() where T : notnull => _provider.GetRequiredService<T>();

    public void Dispose()
    {
        _provider.Dispose();
        SqliteConnection.ClearAllPools();

        try
        {
            System.IO.Directory.Delete(Directory, recursive: true);
        }
        catch (IOException)
        {
            // 临时目录清理失败不影响测试结果
        }
    }
}
