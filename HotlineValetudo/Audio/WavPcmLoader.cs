using System.Buffers.Binary;

namespace HotlineValetudo.Audio;

public static class WavPcmLoader
{
    public static short[] Load(string path)
    {
        using var fs = File.OpenRead(path);
        return Load(fs);
    }

    private static short[] Load(Stream stream)
    {
        var riff = ReadExactly(stream, 4);
        if (!IsAscii(riff, "RIFF"))
            throw new InvalidDataException("Not a WAV file: no RIFF header");

        ReadExactly(stream, 4);

        var wave = ReadExactly(stream, 4);
        if (!IsAscii(wave, "WAVE"))
            throw new InvalidDataException("Not a WAV file: no WAVE header");

        var bitsPerSample = 16;

        while (stream.Position < stream.Length)
        {
            if (stream.Position + 8 > stream.Length)
                throw new InvalidDataException("Truncated WAV chunk header");

            var chunkId = ReadExactly(stream, 4);
            var sizeBytes = ReadExactly(stream, 4);
            var chunkSize = BinaryPrimitives.ReadInt32LittleEndian(sizeBytes);

            if (IsAscii(chunkId, "fmt "))
            {
                if (chunkSize >= 16)
                {
                    var fmtData = ReadExactly(stream, Math.Min(chunkSize, 16));
                    var audioFormat = BinaryPrimitives.ReadInt16LittleEndian(fmtData);
                    bitsPerSample = BinaryPrimitives.ReadInt16LittleEndian(fmtData.AsSpan(14));
                }
                else
                {
                    stream.Seek(chunkSize, SeekOrigin.Current);
                }
            }
            else if (IsAscii(chunkId, "data"))
            {
                if (bitsPerSample != 16)
                    throw new InvalidDataException($"Unsupported bits per sample: {bitsPerSample}");

                var sampleCount = chunkSize / 2;
                var samples = new short[sampleCount];
                var buf = new byte[2];

                for (var i = 0; i < sampleCount; i++)
                {
                    stream.ReadExactly(buf, 0, 2);
                    samples[i] = BinaryPrimitives.ReadInt16LittleEndian(buf);
                }

                return samples;
            }
            else
            {
                stream.Seek(chunkSize, SeekOrigin.Current);
            }
        }

        throw new InvalidDataException("No data chunk found in WAV file");

        static bool IsAscii(byte[] b, string expected)
        {
            if (b.Length != expected.Length) return false;
            for (var i = 0; i < b.Length; i++)
                if (b[i] != (byte)expected[i])
                    return false;
            return true;
        }

        static byte[] ReadExactly(Stream s, int count)
        {
            var buf = new byte[count];
            s.ReadExactly(buf, 0, count);
            return buf;
        }
    }
}