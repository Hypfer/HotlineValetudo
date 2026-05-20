using GroovyCodecs.G711.aLaw;
using GroovyCodecs.G711.uLaw;

namespace HotlineValetudo.Audio;

public static class G711Encode
{
    private static readonly ULawEncoder ULawEncoder = new();
    private static readonly ALawEncoder ALawEncoder = new();

    public static byte[] PcmuEncode(short[] samples)
    {
        return ULawEncoder.Process(samples);
    }

    public static byte[] PcmaEncode(short[] samples)
    {
        return ALawEncoder.Process(samples);
    }
}