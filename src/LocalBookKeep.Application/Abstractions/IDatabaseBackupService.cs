namespace LocalBookKeep.Application.Abstractions;

/// <summary>
/// 数据库文件备份 / 恢复。
/// </summary>
public interface IDatabaseBackupService
{
    /// <summary>当前数据库文件的绝对路径。</summary>
    string DatabasePath { get; }

    /// <summary>把当前数据库复制到指定目录，返回生成的备份文件完整路径。</summary>
    Task<string> BackupAsync(string targetDirectory, CancellationToken cancellationToken = default);

    /// <summary>用指定的备份文件覆盖当前数据库（会覆盖现有数据）。</summary>
    Task RestoreAsync(string backupFilePath, CancellationToken cancellationToken = default);
}
