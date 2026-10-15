using Avalonia.Controls;

namespace LocalBookKeep.Views;

/// <summary>
/// 通用消息框 / 确认框。
/// </summary>
public partial class MessageWindow : Window
{
    /// <summary>构造空弹窗（设计器需要）。</summary>
    public MessageWindow()
    {
        InitializeComponent();
    }

    /// <summary>构造消息框。</summary>
    /// <param name="title">标题。</param>
    /// <param name="message">正文。</param>
    /// <param name="confirmText">确认按钮文本。</param>
    /// <param name="showCancel">是否显示取消按钮（显示则为确认框）。</param>
    public MessageWindow(string title, string message, string confirmText, bool showCancel)
        : this()
    {
        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirmText;
        CancelButton.IsVisible = showCancel;
        ConfirmButton.Click += (_, _) => Close(true);
        CancelButton.Click += (_, _) => Close(false);
    }
}
