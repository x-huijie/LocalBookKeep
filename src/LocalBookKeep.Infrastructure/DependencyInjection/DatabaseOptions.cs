namespace LocalBookKeep.Infrastructure.DependencyInjection;

/// <summary>
/// 数据库提供程序类型。
/// </summary>
public enum DatabaseProvider
{
    /// <summary>本地 SQLite 单文件数据库（V1 默认）。</summary>
    Sqlite = 1,

    /// <summary>MySQL（V2 预留）。</summary>
    MySql = 2
}

/// <summary>
/// 数据库连接配置。切换数据库只需替换这里的 Provider 与连接串。
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>数据库类型。</summary>
    public DatabaseProvider Provider { get; init; } = DatabaseProvider.Sqlite;

    /// <summary>连接字符串。</summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>使用本地 SQLite 文件。</summary>
    public static DatabaseOptions Sqlite(string databaseFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseFilePath);
        return new DatabaseOptions
        {
            Provider = DatabaseProvider.Sqlite,
            ConnectionString = $"Data Source={databaseFilePath}"
        };
    }

    /// <summary>使用 MySQL 连接串（V2 使用）。</summary>
    public static DatabaseOptions MySql(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        return new DatabaseOptions
        {
            Provider = DatabaseProvider.MySql,
            ConnectionString = connectionString
        };
    }
}
