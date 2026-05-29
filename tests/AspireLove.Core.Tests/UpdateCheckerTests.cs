using System.Net;
using AspireLove.Core.Update;

namespace AspireLove.Core.Tests;

public class UpdateCheckerTests
{
    [Theory]
    [InlineData("v1.2.3", 1, 2, 3)]
    [InlineData("1.2.3", 1, 2, 3)]
    [InlineData("V0.1.0", 0, 1, 0)]
    public void TryParseTag_parses_with_and_without_prefix(string tag, int major, int minor, int build)
    {
        Assert.True(UpdateChecker.TryParseTag(tag, out var version));
        Assert.Equal(new Version(major, minor, build), version);
    }

    [Theory]
    [InlineData("latest")]
    [InlineData("")]
    [InlineData("v-broken")]
    public void TryParseTag_rejects_non_versions(string tag) =>
        Assert.False(UpdateChecker.TryParseTag(tag, out _));

    [Fact]
    public async Task CheckAsync_reports_update_when_release_is_newer()
    {
        var http = new HttpClient(new StubHandler(
            """{ "tag_name": "v2.0.0", "html_url": "https://example.com/r", "prerelease": false }"""));

        var result = await new UpdateChecker(http).CheckAsync(new Version(1, 0, 0));

        Assert.NotNull(result);
        Assert.True(result!.UpdateAvailable);
        Assert.Equal(new Version(2, 0, 0), result.Latest);
        Assert.Equal("https://example.com/r", result.ReleaseUrl);
    }

    [Fact]
    public async Task CheckAsync_reports_no_update_when_current_is_latest()
    {
        var http = new HttpClient(new StubHandler(
            """{ "tag_name": "v1.0.0", "html_url": "https://example.com/r", "prerelease": false }"""));

        var result = await new UpdateChecker(http).CheckAsync(new Version(1, 0, 0));

        Assert.NotNull(result);
        Assert.False(result!.UpdateAvailable);
    }

    [Fact]
    public async Task CheckAsync_returns_null_on_http_failure()
    {
        var http = new HttpClient(new StubHandler("", HttpStatusCode.NotFound));

        var result = await new UpdateChecker(http).CheckAsync(new Version(1, 0, 0));

        Assert.Null(result);
    }

    private sealed class StubHandler(string body, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }
}
