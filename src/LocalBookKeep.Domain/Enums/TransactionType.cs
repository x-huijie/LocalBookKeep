namespace LocalBookKeep.Domain.Enums;

/// <summary>
/// 账单收支类型。
/// </summary>
public enum TransactionType
{
    /// <summary>收入。</summary>
    Income = 1,

    /// <summary>支出。</summary>
    Expense = 2
}

/// <summary>
/// <see cref="TransactionType"/> 的扩展方法。
/// </summary>
public static class TransactionTypeExtensions
{
    /// <summary>返回中文显示名称。</summary>
    public static string ToDisplayName(this TransactionType type)
        => type == TransactionType.Income ? "收入" : "支出";

    /// <summary>收入为正 1，支出为 -1，用于计算账户余额与结余。</summary>
    public static int ToSign(this TransactionType type)
        => type == TransactionType.Income ? 1 : -1;
}
