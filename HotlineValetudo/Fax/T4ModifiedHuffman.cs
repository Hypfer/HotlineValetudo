// ITU-T T.4 Modified Huffman (MH) encoding

namespace HotlineValetudo.Fax;

public static partial class T4ModifiedHuffman
{
    public static IEnumerable<bool> EncodeScanline(bool[] line)
    {
        if (line.Length == 0) yield break;

        var runStart = 0;
        var isWhite = true; // Every scanline always starts with a white run (even if length 0)

        while (runStart < line.Length)
        {
            var runEnd = runStart;
            while (runEnd < line.Length && line[runEnd] == isWhite)
                runEnd++;

            var runLength = runEnd - runStart;
            foreach (var bit in EncodeRun(isWhite, runLength))
                yield return bit;

            runStart = runEnd;
            isWhite = !isWhite;
        }
    }

    private static IEnumerable<bool> EncodeRun(bool isWhite, int runLength)
    {
        var table = isWhite ? WhiteTerm : BlackTerm;
        var makeupTable = isWhite ? WhiteMakeup : BlackMakeup;

        if (runLength < 64)
        {
            // Reference mode: direct codeword
            foreach (var bit in EmitCodeword(table[runLength]))
                yield return bit;
        }
        else
        {
            // Makeup mode: multiples of 64 (64, 128, ... 2560) + terminator
            var makeupVal = runLength / 64 * 64;
            if (makeupVal > 2560) makeupVal = 2560; // Max makeup code is 2560

            var makeupIdx = makeupVal / 64 - 1;
            var remaining = runLength - makeupVal;

            foreach (var bit in EmitCodeword(makeupTable[makeupIdx]))
                yield return bit;

            // If still >= 64 (because we capped at 2560), emit more makeups
            if (remaining >= 64)
                foreach (var bit in EncodeRun(isWhite, remaining))
                    yield return bit;
            else
                foreach (var bit in EmitCodeword(table[remaining]))
                    yield return bit;
        }
    }

    private static IEnumerable<bool> EmitCodeword((int value, int length) cw)
    {
        for (var i = cw.length - 1; i >= 0; i--)
            yield return ((cw.value >> i) & 1) == 1;
    }
}