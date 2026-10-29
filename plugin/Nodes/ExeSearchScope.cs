namespace EverythingSearch.Nodes;

/// <summary>
/// Everything 可执行文件作用域：command <c>exe</c>。
/// </summary>
[EavType(EverythingSearchConst.ExeSearchScopeTypeId)]
public sealed class ExeSearchScope : EverythingCategoryScopeBase
{
    public ExeSearchScope() : base(EverythingSearchConst.ExeSearchScopeId)
    {
        Name = "可执行文件";
        Description = "使用 Everything 搜索本机可执行文件与脚本";
    }

    public override Guid MioType => EverythingSearchConst.ExeSearchScopeType;

    public override IReadOnlyList<string> SearchCommands { get; } = ["exe-search"];

    protected override string? QueryFilter => EverythingSearchConst.ExeExtFilter;
}
