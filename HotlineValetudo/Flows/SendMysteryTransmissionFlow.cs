using HotlineValetudo.Fax;
using HotlineValetudo.Pocsag;
using HotlineValetudo.Services;
using HotlineValetudo.Sstv;

namespace HotlineValetudo.Flows;

public class SendMysteryTransmissionFlow : IIvrFlow
{
    private const int SampleRate = 8000;

    private static readonly string[] SstvColorImagePaths =
    [
        Path.Combine(AppContext.BaseDirectory, "Sstv", "images", "confused_valetudog.png"),
        Path.Combine(AppContext.BaseDirectory, "Sstv", "images", "valetudo_banner.png"),
        Path.Combine(AppContext.BaseDirectory, "Sstv", "images", "valetudo_logo.png")
    ];

    private static readonly string[] SstvBwImagePaths =
    [
        Path.Combine(AppContext.BaseDirectory, "Sstv", "images", "scene_presets_bw.png"),
        Path.Combine(AppContext.BaseDirectory, "Sstv", "images", "scene_presets_wb.png"),
        Path.Combine(AppContext.BaseDirectory, "Sstv", "images", "valetudo_bw.png"),
        Path.Combine(AppContext.BaseDirectory, "Sstv", "images", "valetudo_wb.png")
    ];

    private static readonly string[] PocsagMessages =
    [
        "What the fuck did you just fucking say about me, you little bitch? I'll have you know I graduated top of my class in the Navy Seals, and I've been involved in numerous secret raids on Al-Quaeda, and I have over 300 confirmed kills. I am trained in gorilla warfare and I'm the top sniper in the entire US armed forces. You are nothing to me but just another target. I will wipe you the fuck out with precision the likes of which has never been seen before on this Earth, mark my fucking words. You think you can get away with saying that shit to me over the Internet? Think again, fucker. As we speak I am contacting my secret network of spies across the USA and your IP is being traced right now so you better prepare for the storm, maggot. The storm that wipes out the pathetic little thing you call your life. You're fucking dead, kid. I can be anywhere, anytime, and I can kill you in over seven hundred ways, and that's just with my bare hands. Not only am I extensively trained in unarmed combat, but I have access to the entire arsenal of the United States Marine Corps and I will use it to its full extent to wipe your miserable ass off the face of the continent, you little shit. If only you could have known what unholy retribution your little \"clever\" comment was about to bring down upon you, maybe you would have held your fucking tongue. But you couldn't, you didn't, and now you're paying the price, you goddamn idiot. I will shit fury all over you and you will drown in it. You're fucking dead, kiddo."
    ];

    private static readonly string FaxImagePath = Path.Combine(AppContext.BaseDirectory, "Fax", "images", "confused_valetudog.tif");
    private readonly Random _rng = new();

    public ILoggerFactory? LoggerFactory { get; set; }

    public async Task<bool> RunAsync(ICallSession session)
    {
        var logger = LoggerFactory?.CreateLogger<SendMysteryTransmissionFlow>();
        await session.SpeakAsync("Preparing. Please stand by.");
        await Task.Delay(3000);

        var (audio, name) = PickAndGenerate();
        var pcmBytes = PcmToBytes(audio);
        var durationSec = (double)pcmBytes.Length / (SampleRate * 2);
        logger?.LogInformation("Transmission: {Type}, {Samples:N0} samples, {Duration:F1}s, {Bytes:N0} bytes", name,
            audio.Length, durationSec, pcmBytes.Length);
        await session.SendRawAudioAsync(new MemoryStream(pcmBytes));

        await session.SpeakAsync("Transmission complete. Goodbye.");
        await session.HangupAsync();
        return false;
    }

    private (short[] Audio, string Name) PickAndGenerate()
    {
        var choice = _rng.Next(11);
        return choice switch
        {
            0 => (GenerateFaxAudio(), "Fax"),
            1 => (GeneratePocsagAudio(PocsagBaudRate.B512), "POCSAG 512"),
            2 => (GeneratePocsagAudio(PocsagBaudRate.B1200), "POCSAG 1200"),
            3 => (GeneratePocsagAudio(PocsagBaudRate.B2400), "POCSAG 2400"),
            4 => (GenerateSstvColorAudio(SstvEncoder.EncodePD180), "SSTV PD180"),
            5 => (GenerateSstvColorAudio(SstvEncoder.EncodeMartinM1), "SSTV Martin M1"),
            6 => (GenerateSstvColorAudio(SstvEncoder.EncodeScottieS2), "SSTV Scottie S2"),
            7 => (GenerateSstvBwAudio(SstvEncoder.EncodeRobotBW8), "SSTV Robot BW8"),
            8 => (GenerateSstvBwAudio(SstvEncoder.EncodeRobotBW12), "SSTV Robot BW12"),
            9 => (GenerateSstvBwAudio(SstvEncoder.EncodeRobotBW24), "SSTV Robot BW24"),
            10 => (GenerateSstvBwAudio(SstvEncoder.EncodeRobotBW36), "SSTV Robot BW36"),
            _ => (GenerateFaxAudio(), "Fax")
        };
    }

    private short[] GenerateFaxAudio()
    {
        var all = new List<short>();
        all.AddRange(T30ToneGenerator.GenerateHandshake());
        var faxBits = T4Encoder.Encode(FaxImagePath);
        all.AddRange(V27terModulator.ModulateBits(faxBits));
        all.AddRange(T30ToneGenerator.GenerateEndSequence());
        return all.ToArray();
    }

    private short[] GeneratePocsagAudio(PocsagBaudRate baudRate)
    {
        var address = _rng.Next(1, 2097152);
        var message = PickRandomMessage();
        return PocsagFskModulator.ModulateString(address, message, baudRate);
    }

    private short[] GenerateSstvColorAudio(Func<string, int, short[]> encode)
    {
        return encode(SstvColorImagePaths[_rng.Next(SstvColorImagePaths.Length)], SampleRate);
    }

    private short[] GenerateSstvBwAudio(Func<string, int, short[]> encode)
    {
        return encode(SstvBwImagePaths[_rng.Next(SstvBwImagePaths.Length)], SampleRate);
    }

    private string PickRandomMessage()
    {
        return PocsagMessages[_rng.Next(PocsagMessages.Length)];
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