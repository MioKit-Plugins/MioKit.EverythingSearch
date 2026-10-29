namespace EverythingSearch.Nodes;

/// <summary>
/// Everything 图片作用域：command <c>image</c>。
/// </summary>
[EavType(EverythingSearchConst.ImageSearchScopeTypeId)]
public sealed class ImageSearchScope : EverythingCategoryScopeBase
{
    public ImageSearchScope() : base(EverythingSearchConst.ImageSearchScopeId)
    {
        Name = "图片";
        Description = "使用 Everything 搜索本机图片文件";
    }

    public override Guid MioType => EverythingSearchConst.ImageSearchScopeType;

    public override IReadOnlyList<string> SearchCommands { get; } = ["image-search"];

    protected override string? QueryFilter => EverythingSearchConst.ImageExtFilter;
}
