using LocalBookKeep.Application.Dtos;

namespace LocalBookKeep.Application.Abstractions;

/// <summary>
/// 账单导出为 CSV（MiniExcel 实现，可直接用 Excel 打开）。
/// </summary>
public interface ITransactionCsvExporter
{
    /// <summary>导出账单到指定 CSV 文件，返回写入的记录条数。</summary>
    Task<int> ExportAsync(
        IReadOnlyList<TransactionDto> transactions,
        string filePath,
        CancellationToken cancellationToken = default);
}
