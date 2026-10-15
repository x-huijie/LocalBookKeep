using System.Text;
using LocalBookKeep.Application.Abstractions;
using LocalBookKeep.Application.Dtos;
using LocalBookKeep.Domain.Enums;
using MiniExcelLibs;
using MiniExcelLibs.Attributes;

namespace LocalBookKeep.Infrastructure.Export;

/// <summary>
/// 使用 MiniExcel 把账单导出为 CSV。
/// </summary>
/// <remarks>
/// 输出为 UTF-8（带 BOM），保证用 Excel 直接双击打开时中文表头不乱码。
/// </remarks>
public sealed class TransactionCsvExporter : ITransactionCsvExporter
{
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    /// <inheritdoc />
    public async Task<int> ExportAsync(
        IReadOnlyList<TransactionDto> transactions,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var rows = transactions
            .Select(item => new TransactionCsvRow
            {
                Date = item.OccurredOn.ToString("yyyy-MM-dd"),
                Type = item.Type.ToDisplayName(),
                Amount = item.Amount,
                Category = item.CategoryName,
                Account = item.AccountName,
                Remark = item.Remark ?? string.Empty
            })
            .ToList();

        await using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await stream.WriteAsync(Utf8Bom, cancellationToken);
            await MiniExcel.SaveAsAsync(
                stream,
                rows,
                printHeader: true,
                excelType: ExcelType.CSV,
                cancellationToken: cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        return rows.Count;
    }

    private sealed class TransactionCsvRow
    {
        [ExcelColumnName("日期")]
        public string Date { get; init; } = string.Empty;

        [ExcelColumnName("类型")]
        public string Type { get; init; } = string.Empty;

        [ExcelColumnName("金额")]
        public decimal Amount { get; init; }

        [ExcelColumnName("分类")]
        public string Category { get; init; } = string.Empty;

        [ExcelColumnName("账户")]
        public string Account { get; init; } = string.Empty;

        [ExcelColumnName("备注")]
        public string Remark { get; init; } = string.Empty;
    }
}
