using System.Text;
using LocalBookKeep.Application.Abstractions;
using LocalBookKeep.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LocalBookKeep.Infrastructure.Backup;

/// <summary>
/// SQLite 数据库文件备份 / 恢复：直接复制 .db 文件。
/// </summary>
public sealed class SqliteDatabaseBackupService : IDatabaseBackupService
{
    private const string SqliteMagic = "SQLite format 3\0";

    private static readonly string[] SidecarSuffixes = ["-wal", "-shm", "-journal"];

    private readonly BookKeepDbContext _context;

    /// <summary>构造备份服务。</summary>
    public SqliteDatabaseBackupService(BookKeepDbContext context)
    {
        _context = context;
        var dataSource = context.Database.GetDbConnection().DataSource;
        DatabasePath = Path.GetFullPath(dataSource);
    }

    /// <inheritdoc />
    public string DatabasePath { get; }

    /// <inheritdoc />
    public async Task<string> BackupAsync(string targetDirectory, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            throw new ArgumentException("备份目录不能为空。", nameof(targetDirectory));
        }

        Directory.CreateDirectory(targetDirectory);

        var targetPath = Path.Combine(targetDirectory, $"LocalBookKeep_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
        await CopyFileAsync(DatabasePath, targetPath, cancellationToken);
        return targetPath;
    }

    /// <inheritdoc />
    public async Task RestoreAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(backupFilePath))
        {
            throw new ArgumentException("备份文件不存在。", nameof(backupFilePath));
        }

        if (!IsSqliteDatabase(backupFilePath))
        {
            throw new ArgumentException("所选文件不是有效的 SQLite 数据库备份。", nameof(backupFilePath));
        }

        await CopyFileAsync(backupFilePath, DatabasePath, cancellationToken);
    }

    /// <summary>判断文件头是否为 SQLite 数据库。</summary>
    private static bool IsSqliteDatabase(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        Span<byte> header = stackalloc byte[16];
        if (stream.Read(header) != header.Length)
        {
            return false;
        }

        return Encoding.ASCII.GetString(header) == SqliteMagic;
    }

    /// <summary>释放连接池后复制文件，保证复制到的是完整的数据库文件。</summary>
    private async Task CopyFileAsync(string sourcePath, string targetPath, CancellationToken cancellationToken)
    {
        if (string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(targetPath), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // 关闭 EF 连接与 SQLite 连接池，避免文件被占用 / 复制到半写入状态
        try
        {
            _context.Database.CloseConnection();
        }
        catch (InvalidOperationException)
        {
            // 连接尚未打开时无需处理
        }

        SqliteConnection.ClearAllPools();

        var targetDirectory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        await using (var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        await using (var target = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await source.CopyToAsync(target, cancellationToken);
            await target.FlushAsync(cancellationToken);
        }

        // 覆盖恢复时清理旧的日志文件，防止与新数据库内容冲突
        foreach (var suffix in SidecarSuffixes)
        {
            var sidecar = targetPath + suffix;
            if (File.Exists(sidecar))
            {
                File.Delete(sidecar);
            }
        }
    }
}
