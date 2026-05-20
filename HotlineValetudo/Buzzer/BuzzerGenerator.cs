namespace HotlineValetudo.Buzzer;

public static class BuzzerGenerator
{
    private const double TwoPi = 2.0 * Math.PI;
    private const int Amplitude = 8000;
    private const int NoiseAmplitude = 200;
    private const int HumAmplitude = 400;

    public static short[] Generate(int durationSeconds, int sampleRate = 8000, Random? rng = null)
    {
        rng ??= new Random();
        var totalSamples = durationSeconds * sampleRate;
        var samples = new short[totalSamples];

        var phase = 0.0;
        var humPhase = 0.0;
        var env = 0.0;
        var onDuration = 955; // ~0.95 seconds
        var offDuration = rng.Next(1950, 1990); // ~1.95 - 1.99 seconds
        var cycleStartMs = 0;
        var buzzOn = true;

        // IIR filter state for the buzzer tone
        var lpA = Math.Exp(-TwoPi * 3000 / sampleRate);
        double lpY = 0;
        var hpA = Math.Exp(-TwoPi * 80 / sampleRate);
        double hpY = 0;
        double hpXPrev = 0;

        for (var i = 0; i < totalSamples; i++)
        {
            var elapsedMs = (int)(i * 1000.0 / sampleRate);
            var cycleLength = onDuration + offDuration;
            var msInCycle = elapsedMs - cycleStartMs;

            if (msInCycle >= cycleLength)
            {
                cycleStartMs += cycleLength;
                onDuration = 955;
                offDuration = rng.Next(1950, 1990);
                msInCycle = elapsedMs - cycleStartMs;
                cycleLength = onDuration + offDuration;
            }

            buzzOn = msInCycle < onDuration;

            // UVB-76 has a very sharp cutoff and a slightly softer attack
            if (buzzOn)
                env += (1.0 - env) * 0.05; // Quick attack
            else
                env = 0.0; // Instant cutoff

            phase += TwoPi * 115.30 / sampleRate;
            if (phase >= TwoPi) phase -= TwoPi;
            // Replicating the exact overtone structure of UVB-76 derived from spectral matching
            var harmonic = Math.Sin(phase) * 0.49 +
                           Math.Sin(phase * 2) * 1.03 +
                           Math.Sin(phase * 3) * 0.96 +
                           Math.Sin(phase * 4) * 1.21 +
                           Math.Sin(phase * 5) * 1.24 +
                           Math.Sin(phase * 6) * 1.42 +
                           Math.Sin(phase * 7) * 1.16 +
                           Math.Sin(phase * 8) * 0.82 +
                           Math.Sin(phase * 9) * 1.03 +
                           Math.Sin(phase * 10) * 0.94 +
                           Math.Sin(phase * 11) * 0.78;

            // Add soft clipping/overdrive for grit and mechanical distortion
            var drive = 3.33;
            var rawBuzz = Math.Tanh(harmonic * drive) * Amplitude;

            // Apply bandpass filter to the raw buzz BEFORE the envelope
            // This prevents the filter from "ringing" and creating a soft fade-out
            lpY = (1 - lpA) * rawBuzz + lpA * lpY;
            hpY = hpA * (hpY + lpY - hpXPrev);
            hpXPrev = lpY;

            // Apply sharp envelope to the filtered buzz
            var sample = hpY * env;

            humPhase += TwoPi * 50.0 / sampleRate;
            if (humPhase >= TwoPi) humPhase -= TwoPi;
            sample += Math.Sin(humPhase) * HumAmplitude;
            sample += (rng.NextDouble() * 2 - 1) * NoiseAmplitude;

            // Global volume reduction
            sample *= 0.7;

            samples[i] = Clamp(sample, -32768, 32767);
        }

        return samples;
    }

    private static short Clamp(double v, short min, short max)
    {
        return (short)Math.Clamp(v, min, max);
    }
}