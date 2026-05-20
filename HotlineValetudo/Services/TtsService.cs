using System.Text;
using Microsoft.Extensions.Options;
using SherpaOnnx;
using HotlineValetudo.Audio;
using HotlineValetudo.Config;

namespace HotlineValetudo.Services;

public class TtsService : IDisposable
{
    private readonly ILogger<TtsService> _logger;
    private readonly TtsOptions _options;
    private readonly SemaphoreSlim _synthesisLimiter = new(2, 2);
    private readonly OfflineTts _tts;
    private bool _disposed;

    public TtsService(IOptions<TtsOptions> options, ILogger<TtsService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var config = new OfflineTtsConfig();
        config.Model.Vits.Model = _options.ModelPath;
        config.Model.Vits.Tokens = _options.TokensPath;
        config.Model.Vits.DataDir = _options.DataDir;
        config.Model.NumThreads = _options.NumThreads;
        config.Model.Provider = "cpu";

        _logger.LogInformation("Initializing SherpaOnnx TTS (model: {Model})", _options.ModelPath);
        _tts = new OfflineTts(config);
        _logger.LogInformation("TTS initialized (sampleRate: {Sr}, speakers: {N})", _tts.SampleRate, _tts.NumSpeakers);
    }

    public int SampleRate => _tts.SampleRate;

    public void Dispose()
    {
        if (_disposed) return;
        _tts.Dispose();
        _disposed = true;
    }

    public MemoryStream Synthesize(string text)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TtsService));

        var audio = _tts.Generate(text, _options.Speed, _options.SpeakerId);
        var samples = audio.Samples;

        return ConvertToWavStream(samples, audio.SampleRate);
    }

    public short[] SynthesizePcm(string text)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TtsService));

        var audio = _tts.Generate(text, _options.Speed, _options.SpeakerId);
        var samples = audio.Samples;

        var pcm = samples.Select(s => (short)Math.Clamp(s * 32767f, short.MinValue, short.MaxValue)).ToArray();
        return AudioResampler.Resample(pcm, audio.SampleRate, 8000);
    }

    public async Task<short[]> SynthesizePcmAsync(string text, CancellationToken ct = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TtsService));

        await _synthesisLimiter.WaitAsync(ct);
        try
        {
            return await Task.Run(() =>
            {
                var audio = _tts.Generate(text, _options.Speed, _options.SpeakerId);
                var samples = audio.Samples;
                var pcm = samples.Select(s => (short)Math.Clamp(s * 32767f, short.MinValue, short.MaxValue)).ToArray();
                return AudioResampler.Resample(pcm, audio.SampleRate, 8000);
            }, ct);
        }
        finally
        {
            _synthesisLimiter.Release();
        }
    }

    private MemoryStream ConvertToWavStream(float[] samples, int sampleRate)
    {
        var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);

        short channels = 1;
        short bitsPerSample = 16;
        var byteRate = sampleRate * channels * (bitsPerSample / 8);

        writer.Write("RIFF".ToCharArray());
        writer.Write(36 + samples.Length * 2);
        writer.Write("WAVE".ToCharArray());
        writer.Write("fmt ".ToCharArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)(channels * (bitsPerSample / 8)));
        writer.Write(bitsPerSample);
        writer.Write("data".ToCharArray());
        writer.Write(samples.Length * 2);

        foreach (var sample in samples)
        {
            var intSample = (short)Math.Clamp(sample * 32767f, short.MinValue, short.MaxValue);
            writer.Write(intSample);
        }

        stream.Position = 0;
        return stream;
    }
}