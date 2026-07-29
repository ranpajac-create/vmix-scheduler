using System.Net;
using VmixScheduler;

namespace VmixScheduler.Tests;

/// <summary>Records every request URL it receives and returns a canned response — lets tests
/// exercise VmixClient's real HTTP call sequencing/URL building without a real vMix.</summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    public List<string> RequestedUrls { get; } = new();
    private readonly Func<HttpRequestMessage, string> _responder;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, string> responder) => _responder = responder;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestedUrls.Add(request.RequestUri!.ToString());
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(_responder(request))
        };
        return Task.FromResult(response);
    }
}

public class VmixClientTests
{
    private const string SampleStatusXml = """
        <vmix>
          <active>2</active>
          <inputs>
            <input key="key-filler" number="1" title="Filler - C:\Media\song1.mp3" shortTitle="Filler" type="VideoList" state="Running" position="1000" duration="5000">
              <list>
                <item selected="false">C:\Media\song1.mp3</item>
                <item selected="true">C:\Media\song2.mp3</item>
                <item selected="false">C:\Media\song3.mp3</item>
              </list>
            </input>
            <input key="key-ad" number="2" title="Ad@16:30:00" shortTitle="Ad@16:30:00" type="Video" state="Paused" position="0" duration="30000" />
          </inputs>
        </vmix>
        """;

    [Fact]
    public async Task GetStatusAsync_ParsesActiveNumberAndInputAttributes()
    {
        var handler = new FakeHttpMessageHandler(_ => SampleStatusXml);
        var client = new VmixClient(handler);

        var status = await client.GetStatusAsync("127.0.0.1", 8088);

        Assert.Equal("2", status.ActiveNumber);
        Assert.Equal(2, status.Inputs.Count);

        var filler = status.Inputs.Single(i => i.Key == "key-filler");
        Assert.Equal("Filler", filler.ShortTitle);
        Assert.Equal(1000, filler.Position);
        Assert.Equal(5000, filler.Duration);
    }

    [Fact]
    public async Task GetStatusAsync_UsesItemSelectedAttribute_NotInputIndexAttribute()
    {
        var handler = new FakeHttpMessageHandler(_ => SampleStatusXml);
        var client = new VmixClient(handler);

        var status = await client.GetStatusAsync("127.0.0.1", 8088);
        var filler = status.Inputs.Single(i => i.Key == "key-filler");

        Assert.Equal(3, filler.ListItems.Count);
        Assert.Equal(1, filler.SelectedIndex); // "song2.mp3" is the one flagged selected="true"
        Assert.Equal("song2", filler.CurrentSongTitle);
        Assert.Equal("song3", filler.NextSongTitle);
    }

    [Fact]
    public async Task FindActive_ReturnsInputMatchingActiveNumber()
    {
        var handler = new FakeHttpMessageHandler(_ => SampleStatusXml);
        var client = new VmixClient(handler);

        var status = await client.GetStatusAsync("127.0.0.1", 8088);
        var active = status.FindActive();

        Assert.NotNull(active);
        Assert.Equal("key-ad", active!.Key);
    }

    [Fact]
    public async Task TriggerInputAsync_CallsRestartThenCutDirectThenPlay_InOrder()
    {
        var handler = new FakeHttpMessageHandler(_ => "<vmix></vmix>");
        var client = new VmixClient(handler);

        await client.TriggerInputAsync("127.0.0.1", 8088, "some-key");

        Assert.Equal(3, handler.RequestedUrls.Count);
        Assert.Contains("Function=Restart", handler.RequestedUrls[0]);
        Assert.Contains("Function=CutDirect", handler.RequestedUrls[1]);
        Assert.Contains("Function=Play", handler.RequestedUrls[2]);
        Assert.All(handler.RequestedUrls, url => Assert.Contains("Input=some-key", url));
    }

    [Fact]
    public async Task ResumeInputAsync_DoesNotRestart_OnlyCutsAndPlays()
    {
        var handler = new FakeHttpMessageHandler(_ => "<vmix></vmix>");
        var client = new VmixClient(handler);

        await client.ResumeInputAsync("127.0.0.1", 8088, "some-key");

        Assert.Equal(2, handler.RequestedUrls.Count);
        Assert.Contains("Function=CutDirect", handler.RequestedUrls[0]);
        Assert.Contains("Function=Play", handler.RequestedUrls[1]);
        Assert.DoesNotContain(handler.RequestedUrls, url => url.Contains("Restart"));
    }

    [Fact]
    public async Task SetTextAsync_UrlEncodesFieldNameAndValue()
    {
        var handler = new FakeHttpMessageHandler(_ => "<vmix></vmix>");
        var client = new VmixClient(handler);

        await client.SetTextAsync("127.0.0.1", 8088, "key1", "Headline.Text", "Song & Title");

        var url = Assert.Single(handler.RequestedUrls);
        Assert.Contains("Function=SetText", url);
        Assert.Contains("Input=key1", url);
        Assert.Contains("SelectedName=Headline.Text", url);
        Assert.Contains("Value=Song+%26+Title", url);
    }

    [Fact]
    public async Task OverlayOnAsync_TargetsCorrectChannelAndInput()
    {
        var handler = new FakeHttpMessageHandler(_ => "<vmix></vmix>");
        var client = new VmixClient(handler);

        await client.OverlayOnAsync("127.0.0.1", 8088, 2, "key1");

        var url = Assert.Single(handler.RequestedUrls);
        Assert.Contains("Function=OverlayInput2In", url);
        Assert.Contains("Input=key1", url);
    }

    [Fact]
    public async Task OverlayOffAsync_TargetsCorrectChannel()
    {
        var handler = new FakeHttpMessageHandler(_ => "<vmix></vmix>");
        var client = new VmixClient(handler);

        await client.OverlayOffAsync("127.0.0.1", 8088, 3);

        var url = Assert.Single(handler.RequestedUrls);
        Assert.Contains("Function=OverlayInput3Off", url);
    }

    [Fact]
    public async Task LoopListToStartAsync_SelectsIndex1ThenPlays()
    {
        var handler = new FakeHttpMessageHandler(_ => "<vmix></vmix>");
        var client = new VmixClient(handler);

        await client.LoopListToStartAsync("127.0.0.1", 8088, "filler-key");

        Assert.Equal(2, handler.RequestedUrls.Count);
        Assert.Contains("Function=SelectIndex", handler.RequestedUrls[0]);
        Assert.Contains("Value=1", handler.RequestedUrls[0]);
        Assert.Contains("Function=Play", handler.RequestedUrls[1]);
    }
}
