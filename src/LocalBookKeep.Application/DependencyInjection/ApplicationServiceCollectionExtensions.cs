using LocalBookKeep.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LocalBookKeep.Application.DependencyInjection;

/// <summary>
/// 应用层依赖注入注册。
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// 注册账单 / 分类 / 账户 / 汇总用例。
    /// </summary>
    /// <remarks>
    /// 桌面端生命周期与容器相同（应用级单例），业务用例本身无状态，可安全共享。
    /// </remarks>
    public static IServiceCollection AddBookKeepApplication(this IServiceCollection services)
    {
        services.AddSingleton<TransactionService>();
        services.AddSingleton<CategoryService>();
        services.AddSingleton<AccountService>();
        services.AddSingleton<SummaryService>();
        return services;
    }
}
