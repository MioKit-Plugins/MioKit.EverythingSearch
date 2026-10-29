namespace EverythingSearch;

public static class EverythingSearchConst
{
    public const string PluginId = "miokit.everything-search";

    public const string PluginTypeId = "A3A19C16-CB5B-42B6-979F-247572A2BAF0";
    public static readonly Guid PluginType = Guid.Parse(PluginTypeId);

    public const string SearchScopeTypeId = "B7E2C4A1-9F38-4D6E-8C15-2A6D9E1F4B70";
    public static readonly Guid SearchScopeType = Guid.Parse(SearchScopeTypeId);

    public const string AudioSearchScopeTypeId = "D2F3A4B5-6C7D-4E8F-9A0B-1C2D3E4F5A6B";
    public static readonly Guid AudioSearchScopeType = Guid.Parse(AudioSearchScopeTypeId);

    public const string ExeSearchScopeTypeId = "E3A4B5C6-7D8E-4F9A-0B1C-2D3E4F5A6B7C";
    public static readonly Guid ExeSearchScopeType = Guid.Parse(ExeSearchScopeTypeId);

    public const string DocSearchScopeTypeId = "F4B5C6D7-8E9F-4A0B-1C2D-3E4F5A6B7C8D";
    public static readonly Guid DocSearchScopeType = Guid.Parse(DocSearchScopeTypeId);

    public const string FolderSearchScopeTypeId = "A5C6D7E8-9F0A-4B1C-2D3E-4F5A6B7C8D9E";
    public static readonly Guid FolderSearchScopeType = Guid.Parse(FolderSearchScopeTypeId);

    public const string VideoSearchScopeTypeId = "B6D7E8F9-0A1B-4C2D-3E4F-5A6B7C8D9E0F";
    public static readonly Guid VideoSearchScopeType = Guid.Parse(VideoSearchScopeTypeId);

    public const string ArchiveSearchScopeTypeId = "C7E8F9A0-1B2C-4D3E-4F5A-6B7C8D9E0F1A";
    public static readonly Guid ArchiveSearchScopeType = Guid.Parse(ArchiveSearchScopeTypeId);

    public const string ImageSearchScopeTypeId = "D8E9F0A1-2B3C-4E4F-5A6B-7C8D9E0F1A2B";
    public static readonly Guid ImageSearchScopeType = Guid.Parse(ImageSearchScopeTypeId);

    public const string OpenNodeTypeId = "C1D8E5F2-0A47-4B9C-91D3-6E8F2A5B7C90";
    public static readonly Guid OpenNodeType = Guid.Parse(OpenNodeTypeId);

    public const string SearchScopeId = "everything-search-scope";
    public const string AudioSearchScopeId = "everything-audio-scope";
    public const string ExeSearchScopeId = "everything-exe-scope";
    public const string DocSearchScopeId = "everything-doc-scope";
    public const string FolderSearchScopeId = "everything-folder-scope";
    public const string VideoSearchScopeId = "everything-video-scope";
    public const string ArchiveSearchScopeId = "everything-archive-scope";
    public const string ImageSearchScopeId = "everything-image-scope";
    public const string OpenNodeId = "everything-open-node";

    public const uint MaxSearchResults = 100;
    public const string ResultSource = "Everything";

    public const string AudioExtFilter = "ext:mp3;wav;flac;aac;ogg;m4a;wma;ape";
    public const string ExeExtFilter = "ext:exe;com;bat;cmd;msi;ps1";
    public const string DocExtFilter = "ext:pdf;doc;docx;xls;xlsx;ppt;pptx;txt;md";
    public const string FolderFilter = "folder:";
    public const string VideoExtFilter = "ext:mp4;mkv;avi;wmv;mov;flv;webm;m4v;mpeg;mpg;ts";
    public const string ArchiveExtFilter = "ext:zip;rar;7z;tar;gz;bz2;xz;cab";
    public const string ImageExtFilter = "ext:jpg;jpeg;png;gif;bmp;webp;svg;ico;tiff;tif;heic;heif;raw;psd;avif";
}
