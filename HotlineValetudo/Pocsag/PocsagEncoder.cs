namespace HotlineValetudo.Pocsag;

public static class PocsagEncoder
{
    private const uint SyncWord = 0x7CD215D8;
    private const uint IdleWord = 0x7A89C197;
    private const int PreambleLength = 576;
    private const int BatchSize = 16;
    private const int FrameSize = 2;
    private const int CrcBits = 10;
    private const int TextBitsPerWord = 20;
    private const int TextBitsPerChar = 7;
    private const uint CrcPolynomial = 0b11101101001;
    private const uint FlagMessage = 0x100000;
    private const uint FlagTextData = 0x3;

    public static IEnumerable<bool> EncodeTransmission(int address, string message, PocsagBaudRate baudRate)
    {
        var dataWords = EncodeMessageWords(address, message);
        return EncodeBitStream(dataWords);
    }

    private static List<uint> EncodeMessageWords(int address, string message)
    {
        var frameOffset = address & 0x7;
        var idlePrefix = frameOffset * FrameSize;

        var words = new List<uint>();

        for (var i = 0; i < idlePrefix; i++)
            words.Add(IdleWord);

        var addressData = ((uint)(address >> 3) << 2) | FlagTextData;
        words.Add(EncodeCodeword(addressData));

        var dataWords = EncodeAscii(message);
        words.AddRange(dataWords);

        words.Add(IdleWord);

        var remainder = words.Count % BatchSize;
        if (remainder != 0)
        {
            var padding = BatchSize - remainder;
            for (var i = 0; i < padding; i++)
                words.Add(IdleWord);
        }

        return words;
    }

    private static List<uint> EncodeAscii(string message)
    {
        var words = new List<uint>();
        uint currentWord = 0;
        var currentBits = 0;

        foreach (var c in message)
            for (var i = 0; i < TextBitsPerChar; i++)
            {
                currentWord <<= 1;
                currentWord |= (uint)((c >> i) & 1);
                currentBits++;

                if (currentBits == TextBitsPerWord)
                {
                    words.Add(EncodeCodeword(currentWord | FlagMessage));
                    currentWord = 0;
                    currentBits = 0;
                }
            }

        if (currentBits > 0)
        {
            currentWord <<= TextBitsPerWord - currentBits;
            words.Add(EncodeCodeword(currentWord | FlagMessage));
        }

        return words;
    }

    private static uint EncodeCodeword(uint data21)
    {
        var crc = ComputeCrc10(data21);
        var withCrc = (data21 << CrcBits) | crc;
        var parity = ComputeParity(withCrc);
        return (withCrc << 1) | parity;
    }

    private static uint ComputeCrc10(uint data21)
    {
        var denominator = CrcPolynomial << 20;
        var msg = data21 << CrcBits;

        for (var i = 0; i <= 20; i++)
        {
            if (((msg >> (30 - i)) & 1) != 0)
                msg ^= denominator;
            denominator >>= 1;
        }

        return msg & 0x3FF;
    }

    private static uint ComputeParity(uint value31)
    {
        uint p = 0;
        for (var i = 0; i < 31; i++)
        {
            p ^= value31 & 1;
            value31 >>= 1;
        }

        return p;
    }

    private static IEnumerable<bool> EncodeBitStream(List<uint> dataWords)
    {
        for (var i = 0; i < PreambleLength; i++)
            yield return i % 2 == 0;

        foreach (var bit in WordToBits(SyncWord))
            yield return bit;

        var wordIndex = 0;

        while (wordIndex < dataWords.Count)
        {
            for (var i = 0; i < BatchSize && wordIndex < dataWords.Count; i++, wordIndex++)
                foreach (var bit in WordToBits(dataWords[wordIndex]))
                    yield return bit;

            if (wordIndex < dataWords.Count)
                foreach (var bit in WordToBits(SyncWord))
                    yield return bit;
        }
    }

    private static IEnumerable<bool> WordToBits(uint word)
    {
        for (var i = 31; i >= 0; i--)
            yield return ((word >> i) & 1) == 1;
    }
}