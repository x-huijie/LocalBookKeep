using LocalBookKeep.Application.Dtos;
using LocalBookKeep.Domain.Enums;
using LocalBookKeep.Domain.Repositories;

namespace LocalBookKeep.Application.Services;

/// <summary>
/// 汇总统计用例。
/// </summary>
public sealed class SummaryService
{
    private readonly ITransactionRepository _transactions;

    /// <summary>构造汇总服务。</summary>
    public SummaryService(ITransactionRepository transactions)
    {
        _transactions = transactions;
    }

    /// <summary>获取当前月份。</summary>
    public static DateOnly CurrentMonth()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return new DateOnly(today.Year, today.Month, 1);
    }

    /// <summary>统计某个月（或全部月份）的收入 / 支出 / 结余。</summary>
    public async Task<MonthlySummaryDto> GetSummaryAsync(
        DateOnly? month,
        CancellationToken cancellationToken = default)
    {
        DateOnly? from = null;
        DateOnly? to = null;

        if (month is { } value)
        {
            from = new DateOnly(value.Year, value.Month, 1);
            to = new DateOnly(value.Year, value.Month, DateTime.DaysInMonth(value.Year, value.Month));
        }

        var income = await _transactions.GetTotalAmountAsync(TransactionType.Income, from, to, cancellationToken);
        var expense = await _transactions.GetTotalAmountAsync(TransactionType.Expense, from, to, cancellationToken);

        return new MonthlySummaryDto
        {
            Month = month is null ? null : from,
            TotalIncome = income,
            TotalExpense = expense
        };
    }

    /// <summary>获取有账单的月份列表（倒序），并且一定包含当前月份。</summary>
    public async Task<IReadOnlyList<DateOnly>> GetAvailableMonthsAsync(CancellationToken cancellationToken = default)
    {
        var months = await _transactions.GetDistinctMonthsAsync(cancellationToken);
        var set = new SortedSet<DateOnly>(months) { CurrentMonth() };
        return set.Reverse().ToList();
    }
}
