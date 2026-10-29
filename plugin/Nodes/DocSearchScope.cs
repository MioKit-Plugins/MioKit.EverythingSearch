namespace EverythingSearch.Nodes;

/// <summary>
/// Everything 文档作用域：command <c>doc</c>。
/// </summary>
[EavType(EverythingSearchConst.DocSearchScopeTypeId)]
public sealed class DocSearchScope : EverythingCategoryScopeBase
{
    public DocSearchScope() : base(EverythingSearchConst.DocSearchScopeId)
    {
        Name = "文档";
        Description = "使用 Everything 搜索本机文档文件";
    }

    public override Guid MioType => EverythingSearchConst.DocSearchScopeType;

    public override IReadOnlyList<string> SearchCommands { get; } = ["doc-search"];

    protected override string? QueryFilter => EverythingSearchConst.DocExtFilter;
}
