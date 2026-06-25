using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SaturnUI.ViewModels;

/// <summary>
/// ViewModel 基类 - 提供统一的命令执行跟踪、错误处理、UI 线程调度
/// </summary>
public abstract partial class ViewModelBase : ObservableObject, IDisposable
{
    private bool _disposed;

    /// <summary>
    /// 全局加载状态,供 UI 显示加载指示器
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    /// <summary>
    /// 状态文本
    /// </summary>
    [ObservableProperty]
    private string _statusText = "就绪";

    /// <summary>
    /// 错误消息(用于 UI 显示)
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool IsNotBusy => !IsBusy;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// 安全执行异步命令的统一入口,自动处理 IsBusy / ErrorMessage / 异常
    /// </summary>
    protected virtual async Task RunBusyAsync(
        Func<Task> action,
        string? busyText = null,
        string? successText = null)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            if (busyText != null) StatusText = busyText;
            ErrorMessage = null;

            await action();

            if (successText != null) StatusText = successText;
        }
        catch (OperationCanceledException)
        {
            StatusText = "已取消";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusText = "错误";
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected void RaiseError(string message)
    {
        ErrorMessage = message;
        StatusText = "错误";
    }

    protected void ClearError()
    {
        ErrorMessage = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DisposeCore();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 子类重写以释放资源
    /// </summary>
    protected virtual void DisposeCore() { }
}
