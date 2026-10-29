using EverythingSearch.Models;

namespace EverythingSearch.Nodes;

/// <summary>
/// 共享可执行节点：从 SearchResult.Payload 打开文件或在资源管理器中定位。
/// </summary>
[EavType(EverythingSearchConst.OpenNodeTypeId)]
public sealed class EverythingOpenNode : MioObject, IMemoryNodeFeature, IInvokeFeature
{
    public EverythingOpenNode() : base(EverythingSearchConst.OpenNodeId)
    {
        Name = "打开 Everything 结果";
        Description = "打开选中的文件或文件夹";
    }

    public override Guid MioType => EverythingSearchConst.OpenNodeType;

    public Task InvokeAsync(InvokeContext context)
    {
        if (context.SearchResult?.Payload is not EverythingHit hit
            || string.IsNullOrWhiteSpace(hit.FullPath))
        {
            return Task.CompletedTask;
        }

        try
        {
            if (hit.IsFolder)
            {
                if (Directory.Exists(hit.FullPath))
                    ShellHelper.OpenFolderOrSelectFile(hit.FullPath);
            }
            else if (File.Exists(hit.FullPath))
            {
                ShellHelper.OpenFolderOrSelectFile(hit.FullPath);
            }
            else if (Directory.Exists(hit.Directory))
            {
                ShellHelper.OpenFolderOrSelectFile(hit.Directory);
            }
        }
        catch
        {
            // 打开失败时静默忽略，避免打断搜索框
        }

        return Task.CompletedTask;
    }
}
