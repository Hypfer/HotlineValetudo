using HotlineValetudo.Flows;
using HotlineValetudo.Services;

namespace HotlineValetudo.Buzzer;

public class BuzzerFlow : IIvrFlow
{
    private readonly Random _rng = new();

    public ILoggerFactory? LoggerFactory { get; set; }

    public async Task<bool> RunAsync(ICallSession session)
    {
        var logger = LoggerFactory?.CreateLogger<BuzzerFlow>();
        var duration = _rng.Next(20, 45);
        var samples = BuzzerGenerator.Generate(duration, 8000, _rng);
        var bytes = PcmToBytes(samples);
        await session.SendRawAudioAsync(new MemoryStream(bytes));
        await session.HangupAsync();
        logger?.LogInformation("Buzzer: {Duration}s", duration);
        return false;
    }

    private static byte[] PcmToBytes(short[] samples)
    {
        var bytes = new byte[samples.Length * 2];
        for (var i = 0; i < samples.Length; i++)
        {
            bytes[i * 2] = (byte)(samples[i] & 0xFF);
            bytes[i * 2 + 1] = (byte)((samples[i] >> 8) & 0xFF);
        }

        return bytes;
    }
}