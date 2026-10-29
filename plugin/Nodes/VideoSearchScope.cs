namespace EverythingSearch.Nodes;

/// <summary>
/// Everything 视频作用域：command <c>video</c>。
/// </summary>
[EavType(EverythingSearchConst.VideoSearchScopeTypeId)]
public sealed class VideoSearchScope : EverythingCategoryScopeBase
{
    public VideoSearchScope() : base(EverythingSearchConst.VideoSearchScopeId)
    {
        Name = "视频";
        Description = "使用 Everything 搜索本机视频文件";
    }

    public override Guid MioType => EverythingSearchConst.VideoSearchScopeType;

    public override IReadOnlyList<string> SearchCommands { get; } = ["video-search"];

    protected override string? QueryFilter => EverythingSearchConst.VideoExtFilter;
}
