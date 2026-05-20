namespace HotlineValetudo.Audio;

/// <summary>
///     Generates audible DTMF tones as G.711 PCMU audio samples.
///     DTMF uses dual tones: one from low group + one from high group.
/// </summary>
public static class DtmfToneGenerator
{
    private static readonly Dictionary<char, (int low, int high)> DtmfFrequencies = new()
    {
        { '1', (697, 1209) }, { '2', (697, 1336) }, { '3', (697, 1477) }, { 'A', (697, 1633) },
        { '4', (770, 1209) }, { '5', (770, 1336) }, { '6', (770, 1477) }, { 'B', (770, 1633) },
        { '7', (852, 1209) }, { '8', (852, 1336) }, { '9', (852, 1477) }, { 'C', (852, 1633) },
        { '*', (941, 1209) }, { '0', (941, 1336) }, { '#', (941, 1477) }, { 'D', (941, 1633) }
    };

    private static (int low, int high) GetFrequencies(char key)
    {
        return DtmfFrequencies.TryGetValue(key, out var freqs) ? freqs : (697, 1209);
    }

    public static short[] GenerateTone(char key, int sampleRate, int durationMs)
    {
        var (lowFreq, highFreq) = GetFrequencies(key);
        var samplesCount = sampleRate * durationMs / 1000;
        var samples = new short[samplesCount];

        for (var i = 0; i < samplesCount; i++)
        {
            var t = i / (double)sampleRate;
            var sample = Math.Sin(2 * Math.PI * lowFreq * t) + Math.Sin(2 * Math.PI * highFreq * t);
            samples[i] = (short)Math.Clamp((int)(sample * 16000), short.MinValue, short.MaxValue);
        }

        return samples;
    }

    public static short[] GenerateSequence(string digits, int sampleRate = 8000, int durationMs = 100, int gapMs = 50)
    {
        var toneSamples = new List<short>();

        foreach (var digit in digits)
        {
            toneSamples.AddRange(GenerateTone(digit, sampleRate, durationMs));
            toneSamples.AddRange(new short[sampleRate * gapMs / 1000]);
        }

        return toneSamples.ToArray();
    }
}