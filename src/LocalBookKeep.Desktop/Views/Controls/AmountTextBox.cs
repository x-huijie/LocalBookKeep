using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace LocalBookKeep.Views.Controls;

/// <summary>
/// 只允许输入金额的文本框：数字 + 最多一个小数点 + 最多两位小数。
/// </summary>
/// <remarks>
/// 双重防护：
/// 1. 在事件隧道阶段拦截非法字符（字母、汉字、符号），TextBox 收不到这次输入；
/// 2. 文本变化后再统一归一化，处理粘贴、拖拽、重复小数点、小数位超限等情况。
/// </remarks>
public class AmountTextBox : TextBox
{
    /// <summary>整数部分最大位数。</summary>
    public const int MaxIntegerDigits = 9;

    /// <summary>小数部分最大位数。</summary>
    public const int MaxDecimalDigits = 2;

    private bool _isNormalizing;

    /// <summary>构造金额输入框。</summary>
    public AmountTextBox()
    {
        AddHandler(TextInputEvent, OnPreviewTextInput, RoutingStrategies.Tunnel);
        TextChanged += OnTextChangedCore;
    }

    /// <summary>
    /// 复用 <see cref="TextBox"/> 的主题模板。
    /// </summary>
    /// <remarks>
    /// Avalonia 默认用控件自身的运行时类型去查找 ControlTheme，子类如果不重写这里，
    /// 就套不到 TextBox 的模板，控件会渲染成一块没有边框、无法输入的空白区域。
    /// </remarks>
    protected override Type StyleKeyOverride => typeof(TextBox);

    /// <summary>把任意文本清洗为合法的金额文本。</summary>
    public static string Sanitize(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        var hasDecimalPoint = false;
        var decimalCount = 0;
        var integerCount = 0;

        foreach (var ch in text)
        {
            if (char.IsAsciiDigit(ch))
            {
                if (hasDecimalPoint)
                {
                    if (decimalCount >= MaxDecimalDigits)
                    {
                        continue;
                    }

                    decimalCount++;
                }
                else
                {
                    if (integerCount >= MaxIntegerDigits)
                    {
                        continue;
                    }

                    integerCount++;
                }

                builder.Append(ch);
                continue;
            }

            if (ch == '.' && !hasDecimalPoint)
            {
                hasDecimalPoint = true;
                if (builder.Length == 0)
                {
                    builder.Append('0');
                }

                builder.Append('.');
            }
        }

        var result = builder.ToString();
        return TrimLeadingZeros(result);
    }

    private static string TrimLeadingZeros(string value)
    {
        var dotIndex = value.IndexOf('.');
        var integerPart = dotIndex < 0 ? value : value[..dotIndex];

        if (integerPart.Length <= 1 || !integerPart.StartsWith('0'))
        {
            return value;
        }

        var trimmed = integerPart.TrimStart('0');
        return (trimmed.Length == 0 ? "0" : trimmed) + (dotIndex < 0 ? string.Empty : value[dotIndex..]);
    }

    private static bool IsAllowedCharacter(char ch) => char.IsAsciiDigit(ch) || ch == '.';

    private void OnPreviewTextInput(object? sender, TextInputEventArgs e)
    {
        if (e.Handled || string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        foreach (var ch in e.Text)
        {
            if (!IsAllowedCharacter(ch))
            {
                e.Handled = true;
                return;
            }
        }
    }

    private void OnTextChangedCore(object? sender, TextChangedEventArgs e)
    {
        if (_isNormalizing)
        {
            return;
        }

        var text = Text ?? string.Empty;
        var normalized = Sanitize(text);

        if (string.Equals(normalized, text, StringComparison.Ordinal))
        {
            return;
        }

        _isNormalizing = true;
        try
        {
            Text = normalized;
            CaretIndex = normalized.Length;
        }
        finally
        {
            _isNormalizing = false;
        }
    }
}
