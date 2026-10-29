namespace EverythingSearch.Nodes;

/// <summary>
/// Everything 文件夹作用域：command <c>folder</c>。
/// </summary>
[EavType(EverythingSearchConst.FolderSearchScopeTypeId)]
public sealed class FolderSearchScope : EverythingCategoryScopeBase
{
    public FolderSearchScope() : base(EverythingSearchConst.FolderSearchScopeId)
    {
        Name = "文件夹";
        Description = "使用 Everything 搜索本机文件夹";
    }

    public override Guid MioType => EverythingSearchConst.FolderSearchScopeType;

    public override IReadOnlyList<string> SearchCommands { get; } = ["folder-search"];

    protected override string? QueryFilter => EverythingSearchConst.FolderFilter;
}
