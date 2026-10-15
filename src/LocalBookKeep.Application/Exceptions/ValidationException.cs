namespace LocalBookKeep.Application.Exceptions;

/// <summary>
/// 业务校验失败。消息可直接展示给用户。
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>创建校验异常。</summary>
    public ValidationException(string message) : base(message)
    {
    }
}
