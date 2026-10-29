using System.Text.Json;

namespace EverythingSearch.Services;

public sealed class EverythingStatusCallStrategy(IEverythingClientService everything) : IPluginCallStrategy
{
    public string MethodName => "everything.status";

    public int ContractVersion => 1;

    public ValueTask<PluginCallResponse> HandleAsync(
        PluginCallContext context,
        JsonElement? arguments,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (arguments is not null)
            return ValueTask.FromResult(PluginCallResponse.Failure(
                PluginCallErrorCodes.InvalidRequest, "everything.status 不接受参数。"));

        return ValueTask.FromResult(PluginCallResponse.Success(
            JsonSerializer.SerializeToElement(new { available = everything.IsAvailable })));
    }
}

public sealed class EverythingSearchCallStrategy(IEverythingClientService everything) : IPluginCallStrategy
{
    public string MethodName => "everything.search";

    public int ContractVersion => 1;

    public async ValueTask<PluginCallResponse> HandleAsync(
        PluginCallContext context,
        JsonElement? arguments,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (arguments is not { ValueKind: JsonValueKind.Object } value ||
            !value.TryGetProperty("query", out var queryValue) ||
            queryValue.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(queryValue.GetString()) ||
            !value.TryGetProperty("maxResults", out var maxResultsValue) ||
            maxResultsValue.ValueKind != JsonValueKind.Number ||
            !maxResultsValue.TryGetUInt32(out var maxResults) ||
            maxResults is < 1 or > 1000)
        {
            return PluginCallResponse.Failure(
                PluginCallErrorCodes.InvalidRequest,
                "everything.search 要求非空 query 和 1–1000 的 maxResults。");
        }

        var trace = new EverythingSearchTrace(Guid.NewGuid(), 0, $"plugin-call:{context.CallerPluginId}");
        var response = await everything.SearchAsync(
            queryValue.GetString()!, maxResults, trace, cancellationToken);

        var status = response.Status switch
        {
            EverythingSearchStatus.Succeeded => "succeeded",
            EverythingSearchStatus.Unavailable => "unavailable",
            EverythingSearchStatus.Busy => "busy",
            EverythingSearchStatus.TimedOut => "timedOut",
            _ => "failed"
        };
        return PluginCallResponse.Success(JsonSerializer.SerializeToElement(new
        {
            status,
            items = response.Items.Select(hit => new
            {
                fullPath = hit.FullPath,
                name = hit.Name,
                directory = hit.Directory,
                isFolder = hit.IsFolder
            }).ToArray(),
            reason = response.Reason
        }));
    }
}
