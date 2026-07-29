using System.Net;
using System.Xml.Linq;

namespace VmixScheduler;

public class VmixClient
{
    private readonly HttpClient _http;

    public VmixClient() : this(null) { }

    /// <summary>Lets tests inject a fake transport instead of hitting a real vMix over HTTP.</summary>
    public VmixClient(HttpMessageHandler? handler)
    {
        _http = handler != null ? new HttpClient(handler) : new HttpClient();
        _http.Timeout = TimeSpan.FromSeconds(5);
    }

    private static string BaseUrl(string host, int port) => $"http://{host}:{port}";

    public async Task<List<VmixInput>> GetInputsAsync(string host, int port)
    {
        var status = await GetStatusAsync(host, port);
        return status.Inputs;
    }

    public async Task<VmixStatus> GetStatusAsync(string host, int port)
    {
        var url = $"{BaseUrl(host, port)}/api/";
        var xml = await _http.GetStringAsync(url);
        var doc = XDocument.Parse(xml);

        var status = new VmixStatus
        {
            ActiveNumber = doc.Root?.Element("active")?.Value ?? ""
        };

        foreach (var el in doc.Descendants("input"))
        {
            var input = new VmixInput
            {
                Key = el.Attribute("key")?.Value ?? "",
                Number = el.Attribute("number")?.Value ?? "",
                Title = el.Attribute("title")?.Value ?? el.Value,
                ShortTitle = el.Attribute("shortTitle")?.Value ?? "",
                Type = el.Attribute("type")?.Value ?? "",
                State = el.Attribute("state")?.Value ?? "",
                Position = int.TryParse(el.Attribute("position")?.Value, out var pos) ? pos : 0,
                Duration = int.TryParse(el.Attribute("duration")?.Value, out var dur) ? dur : 0
            };

            var listEl = el.Element("list");
            if (listEl != null)
            {
                var items = listEl.Elements("item").ToList();
                input.ListItems = items.Select(li => li.Value).ToList();
                // The input's own "selectedIndex" attribute doesn't reliably line up with the
                // playing item; the item's own selected="true" flag is the source of truth.
                input.SelectedIndex = items.FindIndex(li =>
                    string.Equals(li.Attribute("selected")?.Value, "true", StringComparison.OrdinalIgnoreCase));
            }

            status.Inputs.Add(input);
        }
        return status;
    }

    public async Task SetTextAsync(string host, int port, string inputKey, string fieldName, string value)
    {
        var url = $"{BaseUrl(host, port)}/api/?Function=SetText&Input={WebUtility.UrlEncode(inputKey)}" +
                  $"&SelectedName={WebUtility.UrlEncode(fieldName)}&Value={WebUtility.UrlEncode(value)}";
        using var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();
    }

    public async Task TriggerInputAsync(string host, int port, string inputKey)
    {
        await CallFunctionAsync(host, port, "Restart", inputKey);
        await CallFunctionAsync(host, port, "CutDirect", inputKey);
        await CallFunctionAsync(host, port, "Play", inputKey);
    }

    /// <summary>Cuts to the input and ensures playback, WITHOUT restarting — for continuing a
    /// list input (e.g. Filler) from wherever it currently sits.</summary>
    public async Task ResumeInputAsync(string host, int port, string inputKey)
    {
        await CallFunctionAsync(host, port, "CutDirect", inputKey);
        await CallFunctionAsync(host, port, "Play", inputKey);
    }

    public async Task LoopListToStartAsync(string host, int port, string inputKey)
    {
        await CallFunctionAsync(host, port, "SelectIndex", inputKey, "1"); // vMix list indices are 1-based
        await CallFunctionAsync(host, port, "Play", inputKey);
    }

    public async Task OverlayOnAsync(string host, int port, int channel, string inputKey)
    {
        await CallFunctionAsync(host, port, $"OverlayInput{channel}In", inputKey);
    }

    public async Task OverlayOffAsync(string host, int port, int channel)
    {
        var url = $"{BaseUrl(host, port)}/api/?Function=OverlayInput{channel}Off";
        using var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();
    }

    private async Task CallFunctionAsync(string host, int port, string function, string inputKey, string? value = null)
    {
        var url = $"{BaseUrl(host, port)}/api/?Function={function}&Input={WebUtility.UrlEncode(inputKey)}";
        if (value != null) url += $"&Value={WebUtility.UrlEncode(value)}";
        using var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();
    }
}
