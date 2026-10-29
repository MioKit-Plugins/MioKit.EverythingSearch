namespace EverythingSearch.Nodes;

/// <summary>
/// Everything 压缩文件作用域：command <c>archive</c>。
/// </summary>
[EavType(EverythingSearchConst.ArchiveSearchScopeTypeId)]
public sealed class ArchiveSearchScope : EverythingCategoryScopeBase
{
    public ArchiveSearchScope() : base(EverythingSearchConst.ArchiveSearchScopeId)
    {
        Name = "压缩文件";
        Description = "使用 Everything 搜索本机压缩包";
    }

    public override Guid MioType => EverythingSearchConst.ArchiveSearchScopeType;

    public override IReadOnlyList<string> SearchCommands { get; } = ["archive-search"];

    protected override string? QueryFilter => EverythingSearchConst.ArchiveExtFilter;
}
