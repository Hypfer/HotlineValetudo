using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using HotlineValetudo.Services;

namespace HotlineValetudo.Flows;

public class VersionCheckFlow(HttpClient httpClient) : IIvrFlow
{
    public ILoggerFactory? LoggerFactory { get; set; }

    public async Task<bool> RunAsync(ICallSession session)
    {
        string? version;

        try
        {
            var release =
                await httpClient.GetFromJsonAsync<GithubRelease>(
                    "https://api.github.com/repos/hypfer/valetudo/releases/latest");
            version = release?.TagName ?? "unknown";
        }
        catch
        {
            version = null;
        }

        if (version == null)
        {
            await session.SpeakAsync("The latest version of Valetudo could not be determined. Goodbye.");
            await session.HangupAsync();
            return false;
        }

        await session.SpeakAsync("The latest version of Valetudo is " + version);
        await session.SpeakAsync("In DTMF, that is");

        // Play digits as DTMF tones, dots as TTS "dot"
        var currentDigits = new StringBuilder();
        foreach (var c in version)
            if (char.IsDigit(c))
            {
                currentDigits.Append(c);
            }
            else
            {
                if (currentDigits.Length > 0)
                {
                    await session.PlayDtmfAsync(currentDigits.ToString());
                    currentDigits.Clear();
                }

                await session.SpeakAsync("dot");
            }

        if (currentDigits.Length > 0) await session.PlayDtmfAsync(currentDigits.ToString());

        await session.SpeakAsync("Goodbye.");
        await session.HangupAsync();
        return false;
    }

    private class GithubRelease
    {
        [JsonPropertyName("tag_name")] public string TagName { get; set; } = "";
    }
}