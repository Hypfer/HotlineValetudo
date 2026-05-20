using HotlineValetudo.Fax;
using HotlineValetudo.Services;

namespace HotlineValetudo.Flows;

public class SendFaxFlow(string imagePath) : IIvrFlow
{
    private const int SampleRate = 8000;

    public ILoggerFactory? LoggerFactory { get; set; }

    public async Task<bool> RunAsync(ICallSession session)
    {
        await session.SpeakAsync("Preparing. Please stand by.");
        await Task.Delay(3000);

        var faxAudio = GenerateFaxAudio();
        var pcmBytes = PcmToBytes(faxAudio);
        await session.SendRawAudioAsync(new MemoryStream(pcmBytes));

        await session.SpeakAsync("Transmission complete. Goodbye.");
        await session.HangupAsync();
        return false;
    }

    private short[] GenerateFaxAudio()
    {
        var all = new List<short>();

        // Handshake tones
        all.AddRange(T30ToneGenerator.GenerateHandshake());

        // Encode image and modulate with V.27ter
        var faxBits = T4Encoder.Encode(imagePath);
        all.AddRange(V27terModulator.ModulateBits(faxBits));

        // End sequence
        all.AddRange(T30ToneGenerator.GenerateEndSequence());

        return all.ToArray();
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