using HotlineValetudo.Fax;

namespace HotlineValetudo.Tests.Fax;

public class FaxWavOutputTest
{
    [Test]
    [Explicit("Generates a wav file for manual inspection")]
    public void GenerateTestWav()
    {
        var sampleRate = 8000;
        var imagePath = @"G:\aistuff\valetudo-hotline\HotlineValetudo\Fax\images\confused_valetudog.tif";

        var all = new List<short>();
        all.AddRange(T30ToneGenerator.GenerateHandshake(sampleRate));
        var faxBits = T4Encoder.Encode(imagePath);
        all.AddRange(V27terModulator.ModulateBits(faxBits, sampleRate));
        all.AddRange(T30ToneGenerator.GenerateEndSequence(sampleRate));

        var samples = all.ToArray();

        using var fs = new FileStream(@"G:\aistuff\valetudo-hotline\docs\test_fax.wav", FileMode.Create);
        using var bw = new BinaryWriter(fs);

        bw.Write("RIFF".ToCharArray());
        bw.Write(36 + samples.Length * 2);
        bw.Write("WAVE".ToCharArray());
        bw.Write("fmt ".ToCharArray());
        bw.Write(16);
        bw.Write((short)1); // PCM
        bw.Write((short)1); // Mono
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2);
        bw.Write((short)2); // Block align
        bw.Write((short)16); // Bits per sample
        bw.Write("data".ToCharArray());
        bw.Write(samples.Length * 2);

        foreach (var s in samples) bw.Write(s);
    }
}