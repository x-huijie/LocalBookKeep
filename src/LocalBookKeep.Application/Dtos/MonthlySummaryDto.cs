namespace LocalBookKeep.Application.Dtos;

/// <summary>
/// 月度汇总结果。
/// </summary>
public sealed record MonthlySummaryDto
{
    /// <summary>统计月份；<c>null</c> 表示全部月份。</summary>
    public DateOnly? Month { get; init; }

    /// <summary>收入合计。</summary>
    public decimal TotalIncome { get; init; }

    /// <summary>支出合计。</summary>
    public decimal TotalExpense { get; init; }

    /// <summary>结余 = 收入 - 支出。</summary>
    public decimal Balance => TotalIncome - TotalExpense;
}
