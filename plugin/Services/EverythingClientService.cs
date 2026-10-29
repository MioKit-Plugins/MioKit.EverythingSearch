using System.Diagnostics;
using EverythingSearch.Models;
using EverythingSearchClient;

namespace EverythingSearch.Services;

public enum EverythingSearchStatus
{
    Succeeded,
    Unavailable,
    Busy,
    TimedOut,
    Failed
}

public readonly record struct EverythingSearchTrace(
    Guid SearchId,
    int SearchNumber,
    string ScopeId);

public sealed record EverythingSearchResponse(
    EverythingSearchStatus Status,
    IReadOnlyList<EverythingHit> Items,
    string? Reason = null)
{
    public bool IsSucceeded => Status == EverythingSearchStatus.Succeeded;

    public static EverythingSearchResponse Succeeded(IReadOnlyList<EverythingHit> items) =>
        new(EverythingSearchStatus.Succeeded, items);

    public static EverythingSearchResponse Failed(
        EverythingSearchStatus status,
        string? reason = null) =>
        new(status, [], reason);
}

public interface IEverythingClientService
{
    bool IsAvailable { get; }

    Task<EverythingSearchResponse> SearchAsync(
        string query,
        uint maxResults,
        EverythingSearchTrace trace,
        CancellationToken cancellationToken);
}

internal sealed class EverythingSearchBackendException(
    EverythingSearchStatus status,
    Exception innerException) : Exception(innerException.Message, innerException)
{
    public EverythingSearchStatus Status { get; } = status;
}

public sealed class EverythingClientService : IEverythingClientService
{
    private const uint IpcBusyTimeoutMilliseconds = 2000;
    private static readonly TimeSpan AvailabilityRetryDelay = TimeSpan.FromMilliseconds(80);
    private static readonly TimeSpan IpcReceiveTimeout = TimeSpan.FromMilliseconds(2500);
    private static readonly TimeSpan SearchTimeout = TimeSpan.FromMilliseconds(3500);

    private readonly EverythingSearchPlugin _plugin;
    private readonly SemaphoreSlim _searchGate = new(1, 1);
    private int _lastAvailability = -1;

    public EverythingClientService(EverythingSearchPlugin plugin)
    {
        _plugin = plugin;
    }

    public bool IsAvailable
    {
        get
        {
            try
            {
                var isAvailable = SearchClient.IsEverythingAvailable();
                LogAvailabilityTransition(isAvailable);
                return isAvailable;
            }
            catch (Exception ex)
            {
                LogAvailabilityTransition(false);
                _plugin.Context.Logger.Warning(ex, "检测 Everything IPC 可用性失败。");
                return false;
            }
        }
    }

    public async Task<EverythingSearchResponse> SearchAsync(
        string query,
        uint maxResults,
        EverythingSearchTrace trace,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(query))
            return EverythingSearchResponse.Succeeded([]);

        var totalStopwatch = Stopwatch.StartNew();
        _plugin.Context.Logger.Debug(
            "Everything 搜索开始：SearchId={SearchId}, SearchNumber={SearchNumber}, Scope={Scope}, QueryLength={QueryLength}, MaxResults={MaxResults}",
            trace.SearchId,
            trace.SearchNumber,
            trace.ScopeId,
            query.Length,
            maxResults);

        if (!await EnsureAvailableAsync(trace, cancellationToken))
        {
            LogCompletion(trace, EverythingSearchStatus.Unavailable, 0, totalStopwatch.ElapsedMilliseconds);
            return EverythingSearchResponse.Failed(EverythingSearchStatus.Unavailable);
        }

        var gateStopwatch = Stopwatch.StartNew();
        bool entered;
        try
        {
            entered = await _searchGate.WaitAsync(SearchTimeout, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _plugin.Context.Logger.Debug(
                "Everything 搜索在等待查询通道时取消：SearchId={SearchId}, SearchNumber={SearchNumber}, Scope={Scope}, WaitMs={WaitMs}",
                trace.SearchId,
                trace.SearchNumber,
                trace.ScopeId,
                gateStopwatch.ElapsedMilliseconds);
            throw;
        }

        if (!entered)
        {
            _plugin.Context.Logger.Debug(
                "Everything 搜索等待查询通道超时：SearchId={SearchId}, SearchNumber={SearchNumber}, Scope={Scope}, WaitMs={WaitMs}",
                trace.SearchId,
                trace.SearchNumber,
                trace.ScopeId,
                gateStopwatch.ElapsedMilliseconds);
            return EverythingSearchResponse.Failed(EverythingSearchStatus.TimedOut, "等待上一条 IPC 查询结束超时。");
        }

        _plugin.Context.Logger.Debug(
            "Everything 搜索进入查询通道：SearchId={SearchId}, SearchNumber={SearchNumber}, Scope={Scope}, WaitMs={WaitMs}",
            trace.SearchId,
            trace.SearchNumber,
            trace.ScopeId,
            gateStopwatch.ElapsedMilliseconds);

        var releaseGate = true;
        Task<IReadOnlyList<EverythingHit>>? backendTask = null;
        try
        {
            if (!await EnsureAvailableAsync(trace, cancellationToken))
            {
                LogCompletion(trace, EverythingSearchStatus.Unavailable, 0, totalStopwatch.ElapsedMilliseconds);
                return EverythingSearchResponse.Failed(EverythingSearchStatus.Unavailable);
            }

            backendTask = Task.Run(
                () => SearchCore(query, maxResults, cancellationToken),
                CancellationToken.None);
            var hits = await backendTask.WaitAsync(SearchTimeout, cancellationToken);
            LogCompletion(trace, EverythingSearchStatus.Succeeded, hits.Count, totalStopwatch.ElapsedMilliseconds);
            return EverythingSearchResponse.Succeeded(hits);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (backendTask is { IsCompleted: false })
            {
                releaseGate = false;
                _ = ReleaseGateWhenCompletedAsync(backendTask, trace);
            }

            _plugin.Context.Logger.Debug(
                "Everything 搜索取消：SearchId={SearchId}, SearchNumber={SearchNumber}, Scope={Scope}, ElapsedMs={ElapsedMs}, BackendStillRunning={BackendStillRunning}",
                trace.SearchId,
                trace.SearchNumber,
                trace.ScopeId,
                totalStopwatch.ElapsedMilliseconds,
                !releaseGate);
            throw;
        }
        catch (EverythingSearchBackendException ex)
        {
            var status = IsAvailable ? ex.Status : EverythingSearchStatus.Unavailable;
            _plugin.Context.Logger.Debug(
                ex,
                "Everything 后端查询失败：SearchId={SearchId}, SearchNumber={SearchNumber}, Scope={Scope}, Status={Status}, ElapsedMs={ElapsedMs}",
                trace.SearchId,
                trace.SearchNumber,
                trace.ScopeId,
                status,
                totalStopwatch.ElapsedMilliseconds);
            return EverythingSearchResponse.Failed(status, ex.Message);
        }
        catch (TimeoutException ex)
        {
            if (backendTask is { IsCompleted: false })
            {
                releaseGate = false;
                _ = ReleaseGateWhenCompletedAsync(backendTask, trace);
            }

            _plugin.Context.Logger.Debug(
                ex,
                "Everything 搜索响应超时：SearchId={SearchId}, SearchNumber={SearchNumber}, Scope={Scope}, TimeoutMs={TimeoutMs}, BackendStillRunning={BackendStillRunning}",
                trace.SearchId,
                trace.SearchNumber,
                trace.ScopeId,
                SearchTimeout.TotalMilliseconds,
                !releaseGate);
            return EverythingSearchResponse.Failed(EverythingSearchStatus.TimedOut, ex.Message);
        }
        catch (Exception ex)
        {
            var status = IsAvailable
                ? EverythingSearchStatus.Failed
                : EverythingSearchStatus.Unavailable;
            _plugin.Context.Logger.Warning(
                ex,
                "Everything 搜索异常：SearchId={SearchId}, SearchNumber={SearchNumber}, Scope={Scope}, Status={Status}, ElapsedMs={ElapsedMs}",
                trace.SearchId,
                trace.SearchNumber,
                trace.ScopeId,
                status,
                totalStopwatch.ElapsedMilliseconds);
            return EverythingSearchResponse.Failed(status, ex.Message);
        }
        finally
        {
            if (releaseGate)
                _searchGate.Release();
        }
    }

    private static IReadOnlyList<EverythingHit> SearchCore(
        string query,
        uint maxResults,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var client = new SearchClient
            {
                ReceiveTimeout = IpcReceiveTimeout
            };
            var result = client.Search(
                query,
                SearchClient.SearchFlags.None,
                maxResults,
                offset: 0,
                SearchClient.BehaviorWhenBusy.WaitOrError,
                timeoutMs: IpcBusyTimeoutMilliseconds);

            cancellationToken.ThrowIfCancellationRequested();

            var hits = new List<EverythingHit>((int)result.NumItems);
            foreach (var item in result.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var isFolder = item.Flags.HasFlag(EverythingSearchClient.Result.ItemFlags.Folder)
                    || item.Flags.HasFlag(EverythingSearchClient.Result.ItemFlags.Drive);
                var fullPath = string.IsNullOrEmpty(item.Path)
                    ? item.Name
                    : Path.Combine(item.Path, item.Name);

                hits.Add(new EverythingHit(fullPath, item.Name, item.Path, isFolder));
            }

            return hits;
        }
        catch (EverythingBusyException ex)
        {
            throw new EverythingSearchBackendException(EverythingSearchStatus.Busy, ex);
        }
        catch (ResultsNotReceivedException ex)
        {
            throw new EverythingSearchBackendException(EverythingSearchStatus.TimedOut, ex);
        }
    }

    private async Task ReleaseGateWhenCompletedAsync(Task backendTask, EverythingSearchTrace trace)
    {
        try
        {
            await backendTask;
        }
        catch (Exception ex)
        {
            _plugin.Context.Logger.Debug(
                ex,
                "已取消或超时的 Everything 后端查询结束：SearchId={SearchId}, SearchNumber={SearchNumber}, Scope={Scope}",
                trace.SearchId,
                trace.SearchNumber,
                trace.ScopeId);
        }
        finally
        {
            _searchGate.Release();
            _plugin.Context.Logger.Debug(
                "Everything 查询通道已释放：SearchId={SearchId}, SearchNumber={SearchNumber}, Scope={Scope}",
                trace.SearchId,
                trace.SearchNumber,
                trace.ScopeId);
        }
    }

    private async Task<bool> EnsureAvailableAsync(
        EverythingSearchTrace trace,
        CancellationToken cancellationToken)
    {
        if (IsAvailable)
            return true;

        _plugin.Context.Logger.Debug(
            "Everything IPC 首次探测不可用，准备重试：SearchId={SearchId}, SearchNumber={SearchNumber}, Scope={Scope}, RetryDelayMs={RetryDelayMs}",
            trace.SearchId,
            trace.SearchNumber,
            trace.ScopeId,
            AvailabilityRetryDelay.TotalMilliseconds);

        await Task.Delay(AvailabilityRetryDelay, cancellationToken);
        var isAvailable = IsAvailable;
        _plugin.Context.Logger.Debug(
            "Everything IPC 重试探测完成：SearchId={SearchId}, SearchNumber={SearchNumber}, Scope={Scope}, Available={Available}",
            trace.SearchId,
            trace.SearchNumber,
            trace.ScopeId,
            isAvailable);
        return isAvailable;
    }

    private void LogAvailabilityTransition(bool isAvailable)
    {
        var current = isAvailable ? 1 : 0;
        var previous = Interlocked.Exchange(ref _lastAvailability, current);
        if (previous == current)
            return;

        _plugin.Context.Logger.Information(
            "Everything IPC 可用状态变化：Previous={Previous}, Current={Current}",
            previous < 0 ? "Unknown" : previous == 1 ? "Available" : "Unavailable",
            isAvailable ? "Available" : "Unavailable");
    }

    private void LogCompletion(
        EverythingSearchTrace trace,
        EverythingSearchStatus status,
        int resultCount,
        long elapsedMilliseconds)
    {
        _plugin.Context.Logger.Debug(
            "Everything 搜索结束：SearchId={SearchId}, SearchNumber={SearchNumber}, Scope={Scope}, Status={Status}, ResultCount={ResultCount}, ElapsedMs={ElapsedMs}",
            trace.SearchId,
            trace.SearchNumber,
            trace.ScopeId,
            status,
            resultCount,
            elapsedMilliseconds);
    }
}
