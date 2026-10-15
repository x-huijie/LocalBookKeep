using Avalonia;

namespace LocalBookKeep;

internal static class Program
{
    // 初始化代码。在 AppMain 之前不要使用任何 Avalonia、第三方 API 或依赖同步上下文的代码：
    // 此时框架尚未初始化，可能出现异常。
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia 配置，请勿删除；设计器也会使用。
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
