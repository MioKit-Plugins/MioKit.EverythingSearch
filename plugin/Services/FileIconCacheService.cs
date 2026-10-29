using System.Collections.Concurrent;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace EverythingSearch.Services;

public interface IFileIconCacheService
{
    ValueTask<IImage?> GetIconAsync(
        string fullPath,
        bool isFolder,
        CancellationToken cancellationToken);
}

/// <summary>
/// 进程内小图标缓存：文件夹/常见后缀走 IconSource；exe/lnk/dll 走宿主应用图标；其余按扩展名缓存 Shell 小图标。
/// </summary>
public sealed class FileIconCacheService : IFileIconCacheService
{
    private const int MaxCacheEntries = 512;
    private const int SmallIconPx = 16;

    private readonly Func<IPluginIconService?> _iconsFactory;
    private readonly ConcurrentDictionary<string, IImage> _extensionCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _appIconIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IImage> _appImageCache = new(StringComparer.OrdinalIgnoreCase);
    private int _extensionCacheCount;

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".log", ".csv", ".json", ".xml", ".yml", ".yaml", ".ini", ".cfg", ".cs", ".ts", ".js", ".html", ".css"
    };

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".ico", ".tif", ".tiff", ".svg"
    };

    private static readonly HashSet<string> ArchiveExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".cab"
    };

    private static readonly HashSet<string> AppExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".lnk", ".dll", ".cpl", ".com", ".bat", ".cmd", ".msi", ".ps1"
    };

    public FileIconCacheService(Func<IPluginIconService?> iconsFactory)
    {
        _iconsFactory = iconsFactory;
    }

    public async ValueTask<IImage?> GetIconAsync(
        string fullPath,
        bool isFolder,
        CancellationToken cancellationToken)
    {
        if (isFolder)
            return IconSource.Folder;

        var ext = Path.GetExtension(fullPath);
        if (string.IsNullOrEmpty(ext))
            return IconSource.File;

        if (TextExtensions.Contains(ext))
            return IconSource.TextFile;
        if (ImageExtensions.Contains(ext))
            return IconSource.ImageFile;
        if (ArchiveExtensions.Contains(ext))
            return IconSource.Archive;

        if (AppExtensions.Contains(ext))
            return await GetApplicationIconAsync(fullPath, ext, cancellationToken);

        if (_extensionCache.TryGetValue(ext, out var cached))
            return cached;

        cancellationToken.ThrowIfCancellationRequested();

        var image = await LoadShellSmallIconAsync(fullPath, cancellationToken);
        if (image is null)
            return IconSource.File;

        if (_extensionCache.TryAdd(ext, image))
            Interlocked.Increment(ref _extensionCacheCount);

        return _extensionCache.TryGetValue(ext, out var shared) ? shared : image;
    }

    private async ValueTask<IImage?> GetApplicationIconAsync(
        string fullPath,
        string ext,
        CancellationToken cancellationToken)
    {
        if (_appImageCache.TryGetValue(fullPath, out var cachedImage))
            return cachedImage;

        var fallback = ext.Equals(".lnk", StringComparison.OrdinalIgnoreCase)
            ? IconSource.Shortcut
            : IconSource.Application;

        // 尽量走宿主 Windows 应用图标管线建立持久 IconId；展示用小图标仍取 Shell 小图标并按路径缓存。
        var icons = _iconsFactory();
        if (icons is not null)
        {
            try
            {
                if (!_appIconIds.ContainsKey(fullPath))
                {
                    var isLnk = ext.Equals(".lnk", StringComparison.OrdinalIgnoreCase);
                    var request = isLnk
                        ? new WindowsApplicationIconRequest
                        {
                            IconId = $"everything-lnk:{fullPath}",
                            ShortcutPath = fullPath,
                            TargetSize = SmallIconPx
                        }
                        : new WindowsApplicationIconRequest
                        {
                            IconId = $"everything-app:{fullPath}",
                            ApplicationPath = fullPath,
                            TargetSize = SmallIconPx
                        };

                    var descriptor = await icons.GetOrCreateWindowsApplicationAsync(request, cancellationToken);
                    if (descriptor is not null && !string.IsNullOrWhiteSpace(descriptor.IconId))
                        _appIconIds[fullPath] = descriptor.IconId;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // ignore and fall back to shell icon
            }
        }

        var shellIcon = await LoadShellSmallIconAsync(fullPath, cancellationToken);
        if (shellIcon is not null)
        {
            _appImageCache[fullPath] = shellIcon;
            return shellIcon;
        }

        return fallback;
    }

    private static async Task<IImage?> LoadShellSmallIconAsync(
        string fullPath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var bytes = await FileThumbnailHelper.GetIconAsync(
                fullPath,
                SmallIconPx,
                isFolder: false,
                IconOptions.ReturnIconOnly);

            if (bytes is null || bytes.Length == 0)
                return null;

            return DecodeBitmap(bytes);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static Bitmap? DecodeBitmap(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }
}
