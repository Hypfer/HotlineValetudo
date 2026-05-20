using HotlineValetudo.Flows;
using HotlineValetudo.Services;

namespace HotlineValetudo.NumberStation;

public class NumberStationFlow : IIvrFlow
{
    private static readonly string ClipsDir = Path.Combine(AppContext.BaseDirectory, "Audio", "numberstation");
    private readonly Random _rng = new();

    public ILoggerFactory? LoggerFactory { get; set; }

    public async Task<bool> RunAsync(ICallSession session)
    {
        var logger = LoggerFactory?.CreateLogger<NumberStationFlow>();

        await session.PlayWavAsync(Path.Combine(ClipsDir, "achtung.wav"));
        await Silence(session, _rng.Next(400, 800));

        var groups = _rng.Next(3, 8);

        for (var g = 0; g < groups; g++)
        {
            if (g > 0)
            {
                await session.PlayWavAsync(Path.Combine(ClipsDir, "trennung.wav"));
                await Silence(session, _rng.Next(800, 1500));
            }

            var digits = _rng.Next(3, 7);
            for (var d = 0; d < digits; d++)
            {
                var digit = _rng.Next(0, 10);
                await session.PlayWavAsync(Path.Combine(ClipsDir, $"{digit}.wav"));
                await Silence(session, _rng.Next(200, 700));
            }
        }

        await session.HangupAsync();
        logger?.LogInformation("Number station: {Groups} groups", groups);
        return false;
    }

    private async Task Silence(ICallSession session, int ms)
    {
        var bytes = new byte[ms * 16];
        await session.SendRawAudioAsync(new MemoryStream(bytes));
    }
}