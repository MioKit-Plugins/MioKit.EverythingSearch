using System.Reflection;
using System.Text.Json;
using EverythingSearch.Models;
using EverythingSearch.Services;
using MioKit.Sdk;
using Xunit;

namespace MioKit.EverythingSearch.Tests;

public sealed class EverythingPluginCallStrategiesTests
{
    [Fact]
    public async Task Status_ReturnsBackendAvailability()
    {
        var backend = new FakeBackend { IsAvailable = false };
        var response = await new EverythingStatusCallStrategy(backend)
            .HandleAsync(Caller(), null, TestContext.Current.CancellationToken);

        Assert.Equal(PluginCallStatus.Success, response.Status);
        Assert.False(response.Result!.Value.GetProperty("available").GetBoolean());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public async Task Search_RejectsOutOfRangeResultLimit(uint maxResults)
    {
        var backend = new FakeBackend();
        var arguments = JsonSerializer.SerializeToElement(new { query = "report", maxResults });
        var response = await new EverythingSearchCallStrategy(backend)
            .HandleAsync(Caller(), arguments, TestContext.Current.CancellationToken);

        Assert.Equal(PluginCallStatus.Failure, response.Status);
        Assert.Equal(PluginCallErrorCodes.InvalidRequest, response.ErrorCode);
        Assert.Equal(0, backend.SearchCount);
    }

    [Fact]
    public async Task Search_ReturnsPortableResultsAndCallerTrace()
    {
        var hit = new EverythingHit(@"C:\docs\report.pdf", "report.pdf", @"C:\docs", false);
        var backend = new FakeBackend
        {
            Handler = (_, _, _, _) => Task.FromResult(EverythingSearchResponse.Succeeded([hit]))
        };
        var arguments = JsonSerializer.SerializeToElement(new { query = "report", maxResults = 80 });
        var response = await new EverythingSearchCallStrategy(backend)
            .HandleAsync(Caller(), arguments, TestContext.Current.CancellationToken);

        Assert.Equal(PluginCallStatus.Success, response.Status);
        var result = response.Result!.Value;
        Assert.Equal("succeeded", result.GetProperty("status").GetString());
        Assert.Equal(hit.FullPath, result.GetProperty("items")[0].GetProperty("fullPath").GetString());
        Assert.Equal("plugin-call:miokit.auto-file-select", backend.LastTrace.ScopeId);
    }

    [Theory]
    [InlineData(EverythingSearchStatus.Unavailable, "unavailable")]
    [InlineData(EverythingSearchStatus.Busy, "busy")]
    [InlineData(EverythingSearchStatus.TimedOut, "timedOut")]
    [InlineData(EverythingSearchStatus.Failed, "failed")]
    public async Task Search_PreservesBackendStatus(EverythingSearchStatus status, string expected)
    {
        var backend = new FakeBackend
        {
            Handler = (_, _, _, _) => Task.FromResult(EverythingSearchResponse.Failed(status))
        };
        var arguments = JsonSerializer.SerializeToElement(new { query = "report", maxResults = 80 });
        var response = await new EverythingSearchCallStrategy(backend)
            .HandleAsync(Caller(), arguments, TestContext.Current.CancellationToken);

        Assert.Equal(expected, response.Result!.Value.GetProperty("status").GetString());
        Assert.Equal(0, response.Result.Value.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Search_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var backend = new FakeBackend
        {
            Handler = async (_, _, _, token) =>
            {
                entered.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return EverythingSearchResponse.Succeeded([]);
            }
        };
        var arguments = JsonSerializer.SerializeToElement(new { query = "report", maxResults = 80 });
        var pending = new EverythingSearchCallStrategy(backend)
            .HandleAsync(Caller(), arguments, cancellation.Token).AsTask();
        await entered.Task;
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await pending);
    }

    private static PluginCallContext Caller()
    {
        var constructor = typeof(PluginCallContext).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [typeof(string), typeof(string)],
            null)!;
        return (PluginCallContext)constructor.Invoke(["miokit.auto-file-select", "test-call"]);
    }

    private sealed class FakeBackend : IEverythingClientService
    {
        public bool IsAvailable { get; set; } = true;
        public int SearchCount { get; private set; }
        public EverythingSearchTrace LastTrace { get; private set; }
        public Func<string, uint, EverythingSearchTrace, CancellationToken, Task<EverythingSearchResponse>> Handler { get; set; } =
            (_, _, _, _) => Task.FromResult(EverythingSearchResponse.Succeeded([]));

        public Task<EverythingSearchResponse> SearchAsync(
            string query,
            uint maxResults,
            EverythingSearchTrace trace,
            CancellationToken cancellationToken)
        {
            SearchCount++;
            LastTrace = trace;
            return Handler(query, maxResults, trace, cancellationToken);
        }
    }
}
