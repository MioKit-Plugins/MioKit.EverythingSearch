using EverythingSearch.Models;
using EverythingSearch.Services;
using EverythingSearch.Views;

namespace EverythingSearch.Nodes;

/// <summary>
/// Everything 类别作用域基类：附着后仅本 Scope 搜索；子类提供 command、显示名与查询过滤。
/// 不继承 SearchScopeFeatureBase（其 SearchAsync 为 sealed，且面向子树 ISearchableFeature 委托）。
/// </summary>
public abstract class EverythingCategoryScopeBase : MioObject, ISearchScopeFeature, IMemoryNodeFeature
{
    private static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(140);

    protected EverythingCategoryScopeBase(string id) : base(id)
    {
        PreloadPropertySource =
        [
            ..PreloadPropertySource,
            AttachPanelExtension.UserSearchCommandsProperty
        ];
    }

    public abstract IReadOnlyList<string> SearchCommands { get; }

    /// <summary>
    /// Everything 查询前缀过滤（如 <c>ext:mp3;wav</c>）；为空表示不过滤。
    /// </summary>
    protected abstract string? QueryFilter { get; }

    protected virtual string BuildQuery(string userText)
    {
        if (string.IsNullOrWhiteSpace(QueryFilter))
            return userText;

        return $"{QueryFilter} {userText}";
    }

    public Task InvokeAsync(InvokeContext context)
        => MioIoc.Resolve<ISearchBoxWindow>().SetAttachedPanelAsync(this);

    public Task OnAttachSearchBox(ISearchBoxWindow searchBoxWindow) => Task.CompletedTask;

    public Task OnDetachSearchBox(ISearchBoxWindow searchBoxWindow) => Task.CompletedTask;

    public async ValueTask SearchAsync(
        SearchRequest request,
        IWriteOnlyResultList<SearchResult> writeOnlyResultList,
        CancellationToken cancellationToken)
    {
        if (request.IsEmptyText)
            return;

        await Task.Delay(SearchDebounce, cancellationToken);

        var context = EverythingSearchRegister.Instance.ComponentContext;
        var client = context.Resolve<IEverythingClientService>();
        var openNode = context.Resolve<EverythingOpenNode>();
        var plugin = context.Resolve<EverythingSearchPlugin>();

        var query = BuildQuery(request.SearchText!);
        var trace = new EverythingSearchTrace(request.SearchId, request.SearchNumber, Id);

        EverythingSearchResponse response;
        try
        {
            response = await client.SearchAsync(
                query,
                EverythingSearchConst.MaxSearchResults,
                trace,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            plugin.Context.Logger.Warning(
                ex,
                "Everything 搜索作用域发生未处理异常：SearchId={SearchId}, SearchNumber={SearchNumber}, Scope={Scope}",
                request.SearchId,
                request.SearchNumber,
                Id);
            response = EverythingSearchResponse.Failed(EverythingSearchStatus.Failed, ex.Message);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!response.IsSucceeded)
        {
            writeOnlyResultList.Add(CreateFailureResult(request, openNode, response.Status));
            return;
        }

        foreach (var hit in response.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            writeOnlyResultList.Add(CreateHitResult(request, openNode, hit));
        }
    }

    private static SearchResult CreateFailureResult(
        SearchRequest request,
        EverythingOpenNode openNode,
        EverythingSearchStatus status)
    {
        var (title, description) = status switch
        {
            EverythingSearchStatus.Unavailable =>
                ("Everything 不可用", "请确认 Everything 已安装并正在运行"),
            EverythingSearchStatus.Busy =>
                ("Everything 正忙", "当前查询繁忙，请稍后重试"),
            EverythingSearchStatus.TimedOut =>
                ("Everything 响应超时", "IPC 查询未及时完成，请稍后重试"),
            _ =>
                ("Everything 搜索失败", "搜索发生异常，详细原因已写入日志")
        };

        var result = new SearchResult(request, openNode)
        {
            Title = title,
            Description = description,
            Source = EverythingSearchConst.ResultSource,
            PinMode = SortPriority.Top,
            MatchResult = TextMatchResult.Default(),
            Payload = null
        };
        result.RenderControlFunc = () => new EverythingCompactResultItem { DataContext = result };
        return result;
    }

    private static SearchResult CreateHitResult(
        SearchRequest request,
        EverythingOpenNode openNode,
        EverythingHit hit)
    {
        var result = new SearchResult(request, openNode)
        {
            Title = hit.Name,
            Description = hit.Directory,
            Source = EverythingSearchConst.ResultSource,
            PinMode = SortPriority.Normal,
            MatchResult = TextMatchResult.Success(TextMatchType.Contains, []),
            Payload = hit
        };
        result.RenderControlFunc = () => new EverythingCompactResultItem { DataContext = result };
        return result;
    }
}
