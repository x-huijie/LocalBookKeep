namespace LocalBookKeep.ViewModels;

/// <summary>
/// 月份筛选下拉项。
/// </summary>
/// <param name="Value">月份（每月第一天）；<c>null</c> 表示全部月份。</param>
/// <param name="Label">显示文本。</param>
public sealed record MonthOption(DateOnly? Value, string Label)
{
    /// <summary>“全部月份”选项。</summary>
    public static MonthOption All { get; } = new(null, "全部月份");

    /// <summary>由月份创建选项。</summary>
    public static MonthOption From(DateOnly month) => new(month, $"{month.Year} 年 {month.Month} 月");
}
