namespace EverythingSearch.Nodes;

/// <summary>
/// Everything 全部文件作用域：command <c>everything</c>，无扩展名过滤。
/// </summary>
[EavType(EverythingSearchConst.SearchScopeTypeId)]
public sealed class EverythingSearchScope : EverythingCategoryScopeBase
{
    public EverythingSearchScope() : base(EverythingSearchConst.SearchScopeId)
    {
        Name = "Everything";
        Description = "使用 Everything 搜索本机文件与文件夹";
    }

    public override Guid MioType => EverythingSearchConst.SearchScopeType;

    public override IReadOnlyList<string> SearchCommands { get; } = ["everything-search"];

    protected override string? QueryFilter => null;
}
