using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LocalBookKeep.Application.DependencyInjection;
using LocalBookKeep.Infrastructure.DependencyInjection;
using LocalBookKeep.Infrastructure.Persistence;
using LocalBookKeep.Services;
using LocalBookKeep.ViewModels;
using LocalBookKeep.Views;
using Microsoft.Extensions.DependencyInjection;

namespace LocalBookKeep;

/// <summary>
/// 应用入口：装配依赖注入容器并创建主窗口。
/// </summary>
public partial class App : Avalonia.Application
{
    /// <summary>组合根，供需要时取用服务。</summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var provider = BuildServiceProvider();
            Services = provider;

            TryInitializeDatabase(provider);

            var dialogs = (DialogService)provider.GetRequiredService<IDialogService>();
            var mainWindow = new MainWindow
            {
                DataContext = provider.GetRequiredService<MainViewModel>()
            };
            dialogs.Owner = mainWindow;

            desktop.MainWindow = mainWindow;
            desktop.ShutdownRequested += (_, _) => provider.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // 应用层用例
        services.AddBookKeepApplication();

        // 基础设施层：SQLite 单文件数据库（后续切换 MySQL 只需改这里）
        services.AddBookKeepInfrastructure(DatabaseOptions.Sqlite(ResolveDatabasePath()));

        // 表现层
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<MainViewModel>();

        return services.BuildServiceProvider();
    }

    /// <summary>数据库放在用户本地应用数据目录，避免随程序目录被误删。</summary>
    private static string ResolveDatabasePath()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalBookKeep");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "bookkeep.db");
    }

    private static void TryInitializeDatabase(IServiceProvider provider)
    {
        try
        {
            provider.GetRequiredService<DatabaseInitializer>().InitializeAsync().GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            // 初始化失败时不阻断启动，主界面刷新时会给出具体错误提示
        }
    }
}
