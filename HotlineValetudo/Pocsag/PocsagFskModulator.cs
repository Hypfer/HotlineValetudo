namespace HotlineValetudo.Pocsag;

public static class PocsagFskModulator
{
    private const int MarkFreq = 4050;
    private const int SpaceFreq = 450;

    private static short[] Modulate(IEnumerable<bool> bits, PocsagBaudRate baudRate, int sampleRate = 8000)
    {
        var baud = (int)baudRate;
        var samplesPerBit = sampleRate / baud;
        var bitsList = bits.ToList();
        var samples = new short[bitsList.Count * samplesPerBit];

        var phase = 0.0;
        var phaseInc = 2.0 * Math.PI * MarkFreq / sampleRate;

        for (var i = 0; i < bitsList.Count; i++)
        {
            phaseInc = bitsList[i]
                ? 2.0 * Math.PI * MarkFreq / sampleRate
                : 2.0 * Math.PI * SpaceFreq / sampleRate;

            for (var j = 0; j < samplesPerBit; j++)
            {
                phase += phaseInc;
                if (phase >= 2.0 * Math.PI) phase -= 2.0 * Math.PI;
                samples[i * samplesPerBit + j] = (short)(16000 * Math.Sin(phase));
            }
        }

        return samples;
    }

    public static short[] ModulateString(int address, string message, PocsagBaudRate baudRate, int sampleRate = 8000)
    {
        var bits = PocsagEncoder.EncodeTransmission(address, message, baudRate);
        return Modulate(bits, baudRate, sampleRate);
    }
}