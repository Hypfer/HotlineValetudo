namespace HotlineValetudo.Fax;

public static class V27terModulator
{
    private const int CarrierFreq = 1800;
    private const int Baud = 1200;

    private const int TRAINING_SEG1 = 0;
    private const int TRAINING_SEG2 = 320;
    private const int TRAINING_SEG3 = 352;
    private const int TRAINING_SEG4 = 402;
    private const int TRAINING_SEG5 = 1476;
    private const int TRAINING_END = 1484;

    private static readonly double[] PhaseShifts = { 0, Math.PI / 2, 3 * Math.PI / 2, Math.PI };

    private static readonly double[] PulseShapeCoeffs = { 0.08333, 0.25, 0.33333, 0.25, 0.08333 };

    public static short[] GenerateTraining(int sampleRate = 8000)
    {
        var samplesPerSymbol = sampleRate / Baud;
        var samples = new short[TRAINING_END * samplesPerSymbol];
        var carrierPhase = 0.0;
        var symbolPhase = 0.0;
        var sampleIdx = 0;

        int ScrambleBit(ref uint reg)
        {
            var outBit = (int)(((reg >> 5) ^ (reg >> 6)) & 1);
            reg = (reg << 1) | (uint)outBit;
            return outBit;
        }

        for (var sym = 0; sym < TRAINING_END; sym++)
        {
            if (sym < TRAINING_SEG2)
            {
                symbolPhase = 0.0;
            }
            else if (sym < TRAINING_SEG3)
            {
                for (var j = 0; j < samplesPerSymbol; j++)
                    samples[sampleIdx++] = 0;
                continue;
            }
            else if (sym < TRAINING_SEG4)
            {
                symbolPhase += Math.PI;
                while (symbolPhase >= 2 * Math.PI) symbolPhase -= 2 * Math.PI;
            }
            else if (sym < TRAINING_SEG5)
            {
                uint reg = 0x3C;
                var s0 = ScrambleBit(ref reg);
                var s1 = ScrambleBit(ref reg);
                var s2 = ScrambleBit(ref reg);
                if (s2 == 1)
                {
                    symbolPhase += Math.PI;
                    while (symbolPhase >= 2 * Math.PI) symbolPhase -= 2 * Math.PI;
                }
            }
            else
            {
                symbolPhase = 0.0;
            }

            for (var j = 0; j < samplesPerSymbol; j++)
            {
                carrierPhase += 2 * Math.PI * CarrierFreq / sampleRate;
                if (carrierPhase >= 2 * Math.PI) carrierPhase -= 2 * Math.PI;
                samples[sampleIdx++] = (short)(16000 * Math.Sin(carrierPhase + symbolPhase));
            }
        }

        return samples;
    }

    public static short[] ModulateBits(IEnumerable<bool> bits, int sampleRate = 8000)
    {
        var bitsList = bits.ToList();
        if (bitsList.Count % 2 != 0) bitsList.Add(false);

        uint scrambleReg = 0x3C;
        var patternCount = 0;

        for (var i = 0; i < bitsList.Count; i++)
        {
            var inBit = bitsList[i] ? 1 : 0;
            var outBit = (inBit ^ (int)((scrambleReg >> 5) & 1) ^ (int)((scrambleReg >> 6) & 1)) & 1;

            if (patternCount >= 33)
            {
                outBit ^= 1;
                patternCount = 0;
            }
            else
            {
                if ((((int)((scrambleReg >> 7) & 1) ^ outBit) &
                     ((int)((scrambleReg >> 8) & 1) ^ outBit) &
                     ((int)((scrambleReg >> 11) & 1) ^ outBit) & 1) != 0)
                    patternCount = 0;
                else
                    patternCount++;
            }

            scrambleReg = (scrambleReg << 1) | (uint)outBit;
            bitsList[i] = outBit != 0;
        }

        var symbolCount = bitsList.Count / 2;
        var samplesPerSymbol = sampleRate / Baud;
        var samples = new short[symbolCount * samplesPerSymbol];

        var currentCarrierPhase = 0.0;
        var currentSymbolPhase = 0.0;

        for (var i = 0; i < symbolCount; i++)
        {
            var b1 = bitsList[i * 2] ? 1 : 0;
            var b2 = bitsList[i * 2 + 1] ? 1 : 0;
            var dibit = (b1 << 1) | b2;

            currentSymbolPhase += PhaseShifts[dibit];
            while (currentSymbolPhase >= 2 * Math.PI) currentSymbolPhase -= 2 * Math.PI;

            for (var j = 0; j < samplesPerSymbol; j++)
            {
                var idx = i * samplesPerSymbol + j;
                currentCarrierPhase += 2 * Math.PI * CarrierFreq / sampleRate;
                if (currentCarrierPhase >= 2 * Math.PI) currentCarrierPhase -= 2 * Math.PI;
                samples[idx] = (short)(16000 * Math.Sin(currentCarrierPhase + currentSymbolPhase));
            }
        }

        return ApplyPulseShaping(samples);
    }

    private static short[] ApplyPulseShaping(short[] samples)
    {
        var n = samples.Length;
        var filtered = new short[n];
        var taps = PulseShapeCoeffs.Length;
        var halfTaps = taps / 2;

        for (var i = 0; i < n; i++)
        {
            var sum = 0.0;
            for (var t = 0; t < taps; t++)
            {
                var idx = i + t - halfTaps;
                if (idx < 0) idx = 0;
                if (idx >= n) idx = n - 1;
                sum += samples[idx] * PulseShapeCoeffs[t];
            }

            filtered[i] = (short)Math.Clamp(sum, -32768, 32767);
        }

        return filtered;
    }
}