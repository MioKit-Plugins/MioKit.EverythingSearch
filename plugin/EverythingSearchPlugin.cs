using System.Reflection;
using EverythingSearch.Nodes;
using EverythingSearch.Services;
using EverythingSearchClient;

namespace EverythingSearch;

public class EverythingSearchPlugin : PluginBase
{
    public EverythingSearchPlugin() : base(EverythingSearchConst.PluginId)
    {
    }

    public override Guid MioType => EverythingSearchConst.PluginType;

    protected override async Task StartCoreAsync(CancellationToken cancellationToken)
    {
        var allScope = EnsureScope<EverythingSearchScope>(EverythingSearchConst.SearchScopeId);
        EnsureScope<AudioSearchScope>(EverythingSearchConst.AudioSearchScopeId);
        EnsureScope<ExeSearchScope>(EverythingSearchConst.ExeSearchScopeId);
        EnsureScope<DocSearchScope>(EverythingSearchConst.DocSearchScopeId);
        EnsureScope<FolderSearchScope>(EverythingSearchConst.FolderSearchScopeId);
        EnsureScope<VideoSearchScope>(EverythingSearchConst.VideoSearchScopeId);
        EnsureScope<ArchiveSearchScope>(EverythingSearchConst.ArchiveSearchScopeId);
        EnsureScope<ImageSearchScope>(EverythingSearchConst.ImageSearchScopeId);

        var openNode = this.FindFirstDescendant<EverythingOpenNode>(
            n => n.Id == EverythingSearchConst.OpenNodeId);

        if (openNode is null)
        {
            openNode = Container!.Resolve<EverythingOpenNode>();
            openNode.SetParent(allScope);
        }

        var everythingAssembly = typeof(SearchClient).Assembly;
        var everythingVersion = everythingAssembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? everythingAssembly.GetName().Version?.ToString()
            ?? "Unknown";
        var sdkVersion = typeof(PluginBase).Assembly.GetName().Version?.ToString() ?? "Unknown";
        var client = Container!.Resolve<IEverythingClientService>();

        Context.Logger.Information(
            "Everything 搜索插件已启动：SdkVersion={SdkVersion}, EverythingClientVersion={EverythingClientVersion}, EverythingClientPath={EverythingClientPath}, Available={Available}",
            sdkVersion,
            everythingVersion,
            everythingAssembly.Location,
            client.IsAvailable);

        await Task.CompletedTask;
    }

    private TScope EnsureScope<TScope>(string id)
        where TScope : EverythingCategoryScopeBase
    {
        var scope = this.FindFirstDescendant<TScope>(n => n.Id == id);
        if (scope is null)
        {
            scope = Container!.Resolve<TScope>();
            scope.SetParent(this);
        }

        return scope;
    }

    public override ValueTask<PluginDataCleanupPlan> CreateDataCleanupPlanAsync(
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(new PluginDataCleanupPlan
        {
            AdditionalEavRootObjectIds = []
        });
    }
}
