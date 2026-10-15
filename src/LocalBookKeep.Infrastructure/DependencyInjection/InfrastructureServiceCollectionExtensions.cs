using LocalBookKeep.Application.Abstractions;
using LocalBookKeep.Domain.Repositories;
using LocalBookKeep.Infrastructure.Backup;
using LocalBookKeep.Infrastructure.Export;
using LocalBookKeep.Infrastructure.Persistence;
using LocalBookKeep.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LocalBookKeep.Infrastructure.DependencyInjection;

/// <summary>
/// 基础设施层依赖注入注册。
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// 注册 EF Core 数据上下文、仓储、数据库初始化、备份与导出服务。
    /// </summary>
    public static IServiceCollection AddBookKeepInfrastructure(
        this IServiceCollection services,
        DatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // 桌面端没有请求作用域，统一使用单例，避免长生命周期组件持有已释放的上下文
        services.AddDbContext<BookKeepDbContext>(
            builder => builder.UseBookKeepDatabase(options),
            ServiceLifetime.Singleton,
            ServiceLifetime.Singleton);

        services.AddSingleton<ITransactionRepository, TransactionRepository>();
        services.AddSingleton<ICategoryRepository, CategoryRepository>();
        services.AddSingleton<IAccountRepository, AccountRepository>();

        services.AddSingleton<DatabaseInitializer>();
        services.AddSingleton<ITransactionCsvExporter, TransactionCsvExporter>();
        services.AddSingleton<IDatabaseBackupService, SqliteDatabaseBackupService>();

        return services;
    }

    /// <summary>
    /// 按配置选择数据库提供程序。
    /// </summary>
    /// <remarks>
    /// 切换到 MySQL 时：给基础设施层加上 Pomelo.EntityFrameworkCore.MySql 包，
    /// 然后把下面 <see cref="DatabaseProvider.MySql"/> 分支替换为
    /// <c>builder.UseMySql(options.ConnectionString, ServerVersion.AutoDetect(options.ConnectionString))</c> 即可，
    /// 上层用例与界面无需改动。
    /// </remarks>
    private static DbContextOptionsBuilder UseBookKeepDatabase(
        this DbContextOptionsBuilder builder,
        DatabaseOptions options)
    {
        switch (options.Provider)
        {
            case DatabaseProvider.Sqlite:
                return builder.UseSqlite(options.ConnectionString);

            case DatabaseProvider.MySql:
                throw new NotSupportedException(
                    "MySQL 提供程序尚未启用：请先引用 Pomelo.EntityFrameworkCore.MySql，并在 UseBookKeepDatabase 中启用 UseMySql。");

            default:
                throw new NotSupportedException($"不支持的数据库类型：{options.Provider}。");
        }
    }
}
