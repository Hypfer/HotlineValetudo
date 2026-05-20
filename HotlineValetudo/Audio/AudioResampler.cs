namespace HotlineValetudo.Audio;

public static class AudioResampler
{
    public static short[] Resample(short[] input, int fromSampleRate, int toSampleRate)
    {
        if (fromSampleRate == toSampleRate) return input;

        var ratio = (double)fromSampleRate / toSampleRate;
        var outputLength = (int)(input.Length / ratio);
        var output = new short[outputLength];

        for (var i = 0; i < outputLength; i++)
        {
            var pos = i * ratio;
            var idx = (int)pos;
            var frac = pos - idx;

            if (idx + 1 < input.Length)
                output[i] = (short)(input[idx] * (1 - frac) + input[idx + 1] * frac);
            else
                output[i] = input[idx];
        }

        return output;
    }
}