namespace HotlineValetudo.Fax;

public static class V21Modulator
{
    private const int MarkFreq = 1850;
    private const int SpaceFreq = 1650;
    private const int Baud = 300;

    public static short[] ModulateBits(IEnumerable<bool> bits, int sampleRate = 8000)
    {
        var samplesPerBit = sampleRate / Baud;
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

    public static short[] ModulateBytes(IEnumerable<byte> bytes, int sampleRate = 8000)
    {
        var bits = bytes.SelectMany(b =>
        {
            var result = new bool[8];
            for (var i = 0; i < 8; i++)
                result[i] = ((b >> (7 - i)) & 1) == 1;
            return result;
        });
        return ModulateBits(bits, sampleRate);
    }

    public static short[] ModulateBytes8N1(IEnumerable<byte> bytes, int sampleRate = 8000)
    {
        var bits = new List<bool>();
        foreach (var b in bytes)
        {
            bits.Add(false);
            for (var i = 7; i >= 0; i--)
                bits.Add(((b >> i) & 1) == 1);
            bits.Add(true);
        }

        return ModulateBits(bits, sampleRate);
    }

    public static short[] ModulateHDLC(byte[] data, int sampleRate = 8000)
    {
        var stuffed = HdlcEncode(data);
        return ModulateBits(stuffed, sampleRate);
    }

    private static List<bool> HdlcEncode(byte[] data)
    {
        var crc = CrcCcitt(data, 0xFFFF);
        var payload = new byte[data.Length + 2];
        Array.Copy(data, payload, data.Length);
        payload[data.Length] = (byte)(crc & 0xFF);
        payload[data.Length + 1] = (byte)((crc >> 8) & 0xFF);

        var flagBits = ByteToBitsMsb(0x7E);
        var bits = new List<bool>(flagBits);

        foreach (var b in payload) bits.AddRange(BitStuffedByte(b));

        bits.AddRange(flagBits);
        return bits;
    }

    private static List<bool> BitStuffedByte(byte b)
    {
        var outBits = new List<bool>(10);
        var consecutiveOnes = 0;

        for (var i = 7; i >= 0; i--)
        {
            var bit = ((b >> i) & 1) == 1;
            if (bit)
            {
                consecutiveOnes++;
                outBits.Add(true);
                if (consecutiveOnes == 5)
                {
                    outBits.Add(false);
                    consecutiveOnes = 0;
                }
            }
            else
            {
                consecutiveOnes = 0;
                outBits.Add(false);
            }
        }

        return outBits;
    }

    private static List<bool> ByteToBitsMsb(byte b)
    {
        var bits = new List<bool>(8);
        for (var i = 0; i < 8; i++)
            bits.Add(((b >> (7 - i)) & 1) == 1);
        return bits;
    }

    private static ushort CrcCcitt(byte[] data, ushort init)
    {
        var crc = init;
        foreach (var b in data)
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
                if ((crc & 0x8000) != 0)
                    crc = (ushort)((crc << 1) ^ 0x1021);
                else
                    crc <<= 1;
        }

        return crc;
    }
}