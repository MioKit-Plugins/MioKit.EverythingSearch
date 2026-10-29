namespace EverythingSearch.Nodes;

/// <summary>
/// Everything 音频作用域：command <c>audio</c>。
/// </summary>
[EavType(EverythingSearchConst.AudioSearchScopeTypeId)]
public sealed class AudioSearchScope : EverythingCategoryScopeBase
{
    public AudioSearchScope() : base(EverythingSearchConst.AudioSearchScopeId)
    {
        Name = "音频";
        Description = "使用 Everything 搜索本机音频文件";
    }

    public override Guid MioType => EverythingSearchConst.AudioSearchScopeType;

    public override IReadOnlyList<string> SearchCommands { get; } = ["audio-search"];

    protected override string? QueryFilter => EverythingSearchConst.AudioExtFilter;
}
