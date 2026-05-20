namespace HotlineValetudo.Fax;

public static class T30ToneGenerator
{
    private const byte FcfDis = 0x80;
    private const byte FcfDcs = 0x82;
    private const byte FcfEom = 0x8E;
    private const byte FcfEop = 0x2E;
    private const byte FcfMcf = 0x8C;

    private const byte DisCapability = 0x4F;
    private const byte DcsParameter = 0x95;

    public static short[] GenerateCED(int sampleRate = 8000)
    {
        return GenerateSingleToneDirect(2100, 3300, sampleRate);
    }

    public static short[] GenerateDIS(int sampleRate = 8000)
    {
        return V21Modulator.ModulateHDLC([FcfDis, DisCapability], sampleRate);
    }

    public static short[] GenerateDCS(int sampleRate = 8000)
    {
        return V21Modulator.ModulateHDLC([FcfDcs, DcsParameter], sampleRate);
    }

    public static short[] GenerateEOM(int sampleRate = 8000)
    {
        return V21Modulator.ModulateHDLC([FcfEom], sampleRate);
    }

    public static short[] GenerateMCF(int sampleRate = 8000)
    {
        return V21Modulator.ModulateHDLC([FcfMcf], sampleRate);
    }

    public static short[] GenerateEOP(int sampleRate = 8000)
    {
        return V21Modulator.ModulateHDLC([FcfEop], sampleRate);
    }

    public static short[] GenerateHandshake(int sampleRate = 8000)
    {
        var all = new List<short>();

        all.AddRange(GenerateCED(sampleRate));
        all.AddRange(Silence(600, sampleRate));

        all.AddRange(GenerateDIS(sampleRate));
        all.AddRange(Silence(300, sampleRate));

        all.AddRange(GenerateDCS(sampleRate));
        all.AddRange(Silence(400, sampleRate));

        all.AddRange(GenerateEOP(sampleRate));
        all.AddRange(Silence(200, sampleRate));

        return all.ToArray();
    }

    public static short[] GenerateEndSequence(int sampleRate = 8000)
    {
        var all = new List<short>();

        all.AddRange(GenerateEOP(sampleRate));
        all.AddRange(Silence(300, sampleRate));
        all.AddRange(GenerateEOM(sampleRate));
        all.AddRange(Silence(300, sampleRate));
        all.AddRange(GenerateMCF(sampleRate));

        return all.ToArray();
    }

    private static short[] GenerateSingleToneDirect(int frequency, int durationMs, int sampleRate)
    {
        var samplesCount = sampleRate * durationMs / 1000;
        var samples = new short[samplesCount];
        for (var i = 0; i < samplesCount; i++)
        {
            var t = i / (double)sampleRate;
            samples[i] = (short)(16000 * Math.Sin(2 * Math.PI * frequency * t));
        }

        return samples;
    }

    private static short[] Silence(int durationMs, int sampleRate)
    {
        return new short[sampleRate * durationMs / 1000];
    }
}