using HotlineValetudo.Audio;
using HotlineValetudo.Services;

namespace HotlineValetudo.Flows;

public class HoldMusicFlow : IIvrFlow
{
    private static readonly string[] Messages =
    [
        "We are currently experiencing an unusually high call volume.",
        "Please don't hang up. Hanging up will not make this go away.",
        "We appreciate how important your time is and are glad that you are spending it here!"
    ];

    public ILoggerFactory? LoggerFactory { get; set; }

    public async Task<bool> RunAsync(ICallSession session)
    {
        await session.SpeakAsync(
            "Please hold. We will connect you to an expert as soon as one becomes available.");

        var holdMusicDir = FindHoldMusicDir();
        var rng = new Random();

        var cleanWav = Path.Combine(holdMusicDir, "Chilli.wav");
        if (!File.Exists(cleanWav))
        {
            await session.SpeakAsync("Our hold music files are missing. You will now experience profound silence.");
            while (session.IsActive)
                await Task.Delay(1000, session.CallCancellationToken);
            return false;
        }

        var samples = WavPcmLoader.Load(cleanWav);

        var currentBits = 12;
        const int bitReductionPerLoop = 2;
        var loopCount = 0;

        while (session.IsActive)
        {
            currentBits = Math.Max(1, currentBits);
            await session.PlayBitcrushedPcmAsync(samples, currentBits);
            currentBits -= bitReductionPerLoop;
            loopCount++;

            if (session.IsActive)
            {
                var pool = loopCount >= 4
                    ? [.. Messages, "At this point you should consider whether this is worth your time."]
                    : Messages;
                await session.SpeakAsync(pool[rng.Next(pool.Length)]);
            }
        }

        return false;
    }

    private static string FindHoldMusicDir()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio", "holdmusic");
    }
}