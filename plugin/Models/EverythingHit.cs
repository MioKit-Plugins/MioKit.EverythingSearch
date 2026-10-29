namespace EverythingSearch.Models;

public sealed record EverythingHit(
    string FullPath,
    string Name,
    string Directory,
    bool IsFolder);
