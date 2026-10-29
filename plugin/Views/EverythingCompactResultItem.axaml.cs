using Avalonia.Controls;
using Avalonia.Threading;
using EverythingSearch.Models;
using EverythingSearch.Services;
using MioKit.Sdk;

namespace EverythingSearch.Views;

public partial class EverythingCompactResultItem : UserControl
{
    private CancellationTokenSource? _iconLoadCts;

    public EverythingCompactResultItem()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        DetachedFromVisualTree += (_, _) => CancelIconLoad();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        CancelIconLoad();
        _ = LoadIconAsync();
    }

    private async Task LoadIconAsync()
    {
        if (DataContext is not SearchResult result)
        {
            IconImage.Source = IconSource.File;
            return;
        }

        if (result.Payload is not EverythingHit hit)
        {
            IconImage.Source = IconSource.Default;
            return;
        }

        var cts = new CancellationTokenSource();
        _iconLoadCts = cts;

        try
        {
            var icons = EverythingSearchRegister.Instance.ComponentContext
                .Resolve<IFileIconCacheService>();
            var image = await icons.GetIconAsync(hit.FullPath, hit.IsFolder, cts.Token);
            if (cts.IsCancellationRequested)
                return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!cts.IsCancellationRequested)
                    IconImage.Source = image ?? IconSource.File;
            });
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch
        {
            if (!cts.IsCancellationRequested)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!cts.IsCancellationRequested)
                        IconImage.Source = IconSource.File;
                });
            }
        }
    }

    private void CancelIconLoad()
    {
        try
        {
            _iconLoadCts?.Cancel();
            _iconLoadCts?.Dispose();
        }
        catch
        {
            // ignored
        }

        _iconLoadCts = null;
    }
}
