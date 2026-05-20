using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace HotlineValetudo.Sstv;

public static class SstvEncoder
{
    private const int VisCode = 96;
    private const int Width = 640;
    private const int Height = 496;
    private const int PixelFreqLow = 1500;
    private const int PixelFreqBandwidth = 800;
    private const int Amplitude = 16000;
    private const double TwoPi = 2.0 * Math.PI;

    public static short[] EncodePD180(string imagePath, int sampleRate = 8000)
    {
        using var image = Image.Load(imagePath);
        image.Mutate(x => x.Resize(Width, Height));
        using var rgb = image.CloneAs<Rgb24>();

        var y = new byte[Width * Height];
        var cb = new byte[Width * Height];
        var cr = new byte[Width * Height];

        for (var py = 0; py < Height; py++)
        for (var px = 0; px < Width; px++)
        {
            var pixel = rgb[px, py];
            int r = pixel.R;
            int g = pixel.G;
            int b = pixel.B;

            var yv = Clip(0.299 * r + 0.587 * g + 0.114 * b);
            var cbv = Clip((int)((b - yv) * 0.565 + 128));
            var crv = Clip((int)((r - yv) * 0.713 + 128));

            var idx = py * Width + px;
            y[idx] = (byte)yv;
            cb[idx] = (byte)cbv;
            cr[idx] = (byte)crv;
        }

        var samples = new List<short>();
        var cumulativePhase = 0.0;

        AppendTone(ref samples, ref cumulativePhase, 1900, 300000, sampleRate);
        AppendTone(ref samples, ref cumulativePhase, 1200, 10000, sampleRate);
        AppendTone(ref samples, ref cumulativePhase, 1900, 300000, sampleRate);

        AppendTone(ref samples, ref cumulativePhase, 1200, 30000, sampleRate);

        for (var bit = 0; bit < 8; bit++)
        {
            var freq = ((VisCode >> bit) & 1) == 1 ? 1100 : 1300;
            AppendTone(ref samples, ref cumulativePhase, freq, 30000, sampleRate);
        }

        AppendTone(ref samples, ref cumulativePhase, 1200, 30000, sampleRate);

        var samplesPerPixel = 286.0 / 1000000.0 * sampleRate;
        var pixelAccum = 0.0;

        for (var linePair = 0; linePair < Height / 2; linePair++)
        {
            var evenLine = linePair * 2;
            var oddLine = evenLine + 1;

            AppendTone(ref samples, ref cumulativePhase, 1200, 20000, sampleRate);
            AppendTone(ref samples, ref cumulativePhase, 1500, 2080, sampleRate);

            AppendPixels(ref samples, ref cumulativePhase, ref pixelAccum, y, evenLine, Width, samplesPerPixel,
                sampleRate);
            AppendPixelsAveraged(ref samples, ref cumulativePhase, ref pixelAccum, cr, evenLine, oddLine, Width,
                samplesPerPixel, sampleRate);
            AppendPixelsAveraged(ref samples, ref cumulativePhase, ref pixelAccum, cb, evenLine, oddLine, Width,
                samplesPerPixel, sampleRate);
            AppendPixels(ref samples, ref cumulativePhase, ref pixelAccum, y, oddLine, Width, samplesPerPixel,
                sampleRate);
        }

        return samples.ToArray();
    }

    private static void AppendTone(ref List<short> samples, ref double phase, int frequencyHz, int durationUs,
        int sampleRate)
    {
        var numSamples = durationUs * sampleRate / 1000000;
        var phaseInc = TwoPi * frequencyHz / sampleRate;

        for (var i = 0; i < numSamples; i++)
        {
            phase += phaseInc;
            if (phase >= TwoPi) phase -= TwoPi;
            samples.Add((short)(Amplitude * Math.Sin(phase)));
        }
    }

    private static void AppendPixels(ref List<short> samples, ref double phase, ref double accum, byte[] channel,
        int line, int width, double samplesPerPixel, int sampleRate)
    {
        for (var x = 0; x < width; x++)
        {
            int val = channel[line * width + x];
            var freq = PixelFreqLow + val / 255.0 * PixelFreqBandwidth;
            var phaseInc = TwoPi * freq / sampleRate;

            accum += samplesPerPixel;
            while (accum >= 1.0)
            {
                accum -= 1.0;
                phase += phaseInc;
                if (phase >= TwoPi) phase -= TwoPi;
                samples.Add((short)(Amplitude * Math.Sin(phase)));
            }
        }
    }

    private static void AppendPixelsAveraged(ref List<short> samples, ref double phase, ref double accum,
        byte[] channel, int line0, int line1, int width, double samplesPerPixel, int sampleRate)
    {
        for (var x = 0; x < width; x++)
        {
            var avg = (channel[line0 * width + x] + channel[line1 * width + x]) / 2;
            var freq = PixelFreqLow + avg / 255.0 * PixelFreqBandwidth;
            var phaseInc = TwoPi * freq / sampleRate;

            accum += samplesPerPixel;
            while (accum >= 1.0)
            {
                accum -= 1.0;
                phase += phaseInc;
                if (phase >= TwoPi) phase -= TwoPi;
                samples.Add((short)(Amplitude * Math.Sin(phase)));
            }
        }
    }

    public static short[] EncodeMartinM1(string imagePath, int sampleRate = 8000)
    {
        const int visCode = 172;
        const int width = 320;
        const int height = 256;
        const int syncDurationUs = 4862;
        const int porchDurationUs = 572;
        const double pixelDurationUs = 457.6;

        using var image = Image.Load(imagePath);
        image.Mutate(x => x.Resize(width, height));
        using var rgb = image.CloneAs<Rgb24>();

        var r = new byte[width * height];
        var g = new byte[width * height];
        var b = new byte[width * height];

        for (var py = 0; py < height; py++)
        for (var px = 0; px < width; px++)
        {
            var pixel = rgb[px, py];
            var idx = py * width + px;
            r[idx] = pixel.R;
            g[idx] = pixel.G;
            b[idx] = pixel.B;
        }

        var samples = new List<short>();
        var cumulativePhase = 0.0;

        AppendTone(ref samples, ref cumulativePhase, 1900, 300000, sampleRate);
        AppendTone(ref samples, ref cumulativePhase, 1200, 10000, sampleRate);
        AppendTone(ref samples, ref cumulativePhase, 1900, 300000, sampleRate);

        AppendTone(ref samples, ref cumulativePhase, 1200, 30000, sampleRate);

        for (var bit = 0; bit < 8; bit++)
        {
            var freq = ((visCode >> bit) & 1) == 1 ? 1100 : 1300;
            AppendTone(ref samples, ref cumulativePhase, freq, 30000, sampleRate);
        }

        AppendTone(ref samples, ref cumulativePhase, 1200, 30000, sampleRate);

        var samplesPerPixel = pixelDurationUs / 1000000.0 * sampleRate;
        var pixelAccum = 0.0;

        for (var line = 0; line < height; line++)
        {
            AppendTone(ref samples, ref cumulativePhase, 1200, syncDurationUs, sampleRate);
            AppendTone(ref samples, ref cumulativePhase, 1500, porchDurationUs, sampleRate);
            AppendPixels(ref samples, ref cumulativePhase, ref pixelAccum, g, line, width, samplesPerPixel, sampleRate);
            AppendTone(ref samples, ref cumulativePhase, 1500, porchDurationUs, sampleRate);
            AppendPixels(ref samples, ref cumulativePhase, ref pixelAccum, b, line, width, samplesPerPixel, sampleRate);
            AppendTone(ref samples, ref cumulativePhase, 1500, porchDurationUs, sampleRate);
            AppendPixels(ref samples, ref cumulativePhase, ref pixelAccum, r, line, width, samplesPerPixel, sampleRate);
            AppendTone(ref samples, ref cumulativePhase, 1500, porchDurationUs, sampleRate);
        }

        return samples.ToArray();
    }

    public static short[] EncodeScottieS2(string imagePath, int sampleRate = 8000)
    {
        const int visCode = 184;
        const int width = 320;
        const int height = 256;
        const int syncDurationUs = 9000;
        const int porchDurationUs = 1500;
        const double pixelDurationUs = 275.2;

        using var image = Image.Load(imagePath);
        image.Mutate(x => x.Resize(width, height));
        using var rgb = image.CloneAs<Rgb24>();

        var r = new byte[width * height];
        var g = new byte[width * height];
        var b = new byte[width * height];

        for (var py = 0; py < height; py++)
        for (var px = 0; px < width; px++)
        {
            var pixel = rgb[px, py];
            var idx = py * width + px;
            r[idx] = pixel.R;
            g[idx] = pixel.G;
            b[idx] = pixel.B;
        }

        var samples = new List<short>();
        var cumulativePhase = 0.0;

        AppendTone(ref samples, ref cumulativePhase, 1900, 300000, sampleRate);
        AppendTone(ref samples, ref cumulativePhase, 1200, 10000, sampleRate);
        AppendTone(ref samples, ref cumulativePhase, 1900, 300000, sampleRate);

        AppendTone(ref samples, ref cumulativePhase, 1200, 30000, sampleRate);

        for (var bit = 0; bit < 8; bit++)
        {
            var freq = ((visCode >> bit) & 1) == 1 ? 1100 : 1300;
            AppendTone(ref samples, ref cumulativePhase, freq, 30000, sampleRate);
        }

        AppendTone(ref samples, ref cumulativePhase, 1200, 30000, sampleRate);

        var samplesPerPixel = pixelDurationUs / 1000000.0 * sampleRate;
        var pixelAccum = 0.0;

        AppendTone(ref samples, ref cumulativePhase, 1200, syncDurationUs, sampleRate);

        for (var line = 0; line < height; line++)
        {
            AppendTone(ref samples, ref cumulativePhase, 1500, porchDurationUs, sampleRate);
            AppendPixels(ref samples, ref cumulativePhase, ref pixelAccum, g, line, width, samplesPerPixel, sampleRate);
            AppendTone(ref samples, ref cumulativePhase, 1500, porchDurationUs, sampleRate);
            AppendPixels(ref samples, ref cumulativePhase, ref pixelAccum, b, line, width, samplesPerPixel, sampleRate);
            AppendTone(ref samples, ref cumulativePhase, 1200, syncDurationUs, sampleRate);
            AppendTone(ref samples, ref cumulativePhase, 1500, porchDurationUs, sampleRate);
            AppendPixels(ref samples, ref cumulativePhase, ref pixelAccum, r, line, width, samplesPerPixel, sampleRate);
        }

        return samples.ToArray();
    }

    private static short[] EncodeRobotBw(string imagePath, int visCode, int width, int height, int syncDurationUs,
        double pixelDurationUs, int sampleRate)
    {
        using var image = Image.Load(imagePath);
        image.Mutate(x => x.Resize(width, height));
        using var gray = image.CloneAs<L8>();

        var y = new byte[width * height];
        for (var i = 0; i < width * height; i++) y[i] = gray[i % width, i / width].PackedValue;

        var samples = new List<short>();
        var cumulativePhase = 0.0;

        AppendTone(ref samples, ref cumulativePhase, 1900, 300000, sampleRate);
        AppendTone(ref samples, ref cumulativePhase, 1200, 10000, sampleRate);
        AppendTone(ref samples, ref cumulativePhase, 1900, 300000, sampleRate);

        AppendTone(ref samples, ref cumulativePhase, 1200, 30000, sampleRate);

        for (var bit = 0; bit < 8; bit++)
        {
            var freq = ((visCode >> bit) & 1) == 1 ? 1100 : 1300;
            AppendTone(ref samples, ref cumulativePhase, freq, 30000, sampleRate);
        }

        AppendTone(ref samples, ref cumulativePhase, 1200, 30000, sampleRate);

        var samplesPerPixel = pixelDurationUs / 1000000.0 * sampleRate;
        var pixelAccum = 0.0;

        for (var line = 0; line < height; line++)
        {
            AppendTone(ref samples, ref cumulativePhase, 1200, syncDurationUs, sampleRate);
            AppendPixels(ref samples, ref cumulativePhase, ref pixelAccum, y, line, width, samplesPerPixel, sampleRate);
        }

        return samples.ToArray();
    }

    public static short[] EncodeRobotBW8(string imagePath, int sampleRate = 8000)
    {
        return EncodeRobotBw(imagePath, 129, 160, 120, 10000, 350.0, sampleRate);
    }

    public static short[] EncodeRobotBW12(string imagePath, int sampleRate = 8000)
    {
        return EncodeRobotBw(imagePath, 5, 160, 120, 7000, 581.25, sampleRate);
    }

    public static short[] EncodeRobotBW24(string imagePath, int sampleRate = 8000)
    {
        return EncodeRobotBw(imagePath, 9, 320, 240, 12000, 290.625, sampleRate);
    }

    public static short[] EncodeRobotBW36(string imagePath, int sampleRate = 8000)
    {
        return EncodeRobotBw(imagePath, 141, 320, 240, 12000, 431.25, sampleRate);
    }

    private static int Clip(double v)
    {
        return v < 0 ? 0 : v > 255 ? 255 : (int)Math.Round(v);
    }
}