using System.Net;
using System.Text;
using System.Threading.Channels;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using SIPSorceryMedia.Abstractions;
using HotlineValetudo.Audio;

namespace HotlineValetudo.Services;

public readonly struct DtmfResult
{
    public readonly bool HasDigit;
    public char Digit { get; }

    public bool IsTimeout => !HasDigit;
    public bool TimedOut => IsTimeout;

    private DtmfResult(bool hasDigit, char digit)
    {
        HasDigit = hasDigit;
        Digit = digit;
    }

    public static DtmfResult Timeout => new(false, default);

    public static DtmfResult FromDigit(char d)
    {
        return new DtmfResult(true, d);
    }
}

public interface ICallSession
{
    Guid Id { get; }
    bool IsActive { get; }
    CancellationToken CallCancellationToken { get; }
    Task<DtmfResult> ReadPromptAsync(string text, TimeSpan timeout);
    Task<DtmfResult> ReadPromptSegmentsAsync(IEnumerable<string> segments, TimeSpan timeout);
    Task<DtmfResult> WaitForDtmfAsync(TimeSpan timeout);
    Task SpeakAsync(string text);
    Task PlayDtmfAsync(string digits);
    Task<string> CollectDigitsAsync(char terminator, TimeSpan timeout);
    Task HangupAsync();
    void DrainPendingDtmf();
    Task SendRawAudioAsync(Stream pcmStream);
    Task PlayWavAsync(string path);
    Task PlayBitcrushedPcmAsync(short[] samples, int bits);
}

public class CallSession : ICallSession, IAsyncDisposable
{
    private readonly AudioExtrasSource _audioExtras;
    private readonly CancellationTokenSource _callCts = new();
    private readonly object _debounceLock = new();
    private readonly Channel<char> _dtmfChannel;
    private readonly ILogger<CallSession> _logger;
    private readonly RTPSession _rtpSession;

    private readonly SIPUserAgent _sipUserAgent;
    private readonly TtsService _ttsService;
    private readonly SIPServerUserAgent _uas;
    private bool _isDisposed;
    private char _lastDtmfDigit;
    private DateTime _lastDtmfTime;

    public CallSession(SIPUserAgent sipUserAgent, SIPServerUserAgent uas, IMediaSession mediaSession,
        TtsService ttsService, ILogger<CallSession> logger)
    {
        _sipUserAgent = sipUserAgent;
        _uas = uas;
        _rtpSession = (RTPSession)mediaSession;
        _audioExtras = ((VoIPMediaSession)mediaSession).AudioExtrasSource;
        _ttsService = ttsService;
        _logger = logger;

        _dtmfChannel = Channel.CreateBounded<char>(new BoundedChannelOptions(4)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

        _rtpSession.OnRtpEvent += OnRtpEvent;
        _sipUserAgent.OnCallHungup += OnCallHungup;
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _callCts.Cancel();
        _rtpSession.OnRtpEvent -= OnRtpEvent;
        _sipUserAgent.OnCallHungup -= OnCallHungup;
        _dtmfChannel.Writer.Complete();
        await _dtmfChannel.Reader.Completion;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public bool IsActive => !_callCts.IsCancellationRequested && !_isDisposed;
    public CancellationToken CallCancellationToken => _callCts.Token;

    public async Task<DtmfResult> ReadPromptAsync(string text, TimeSpan timeout)
    {
        await WaitForAudioDestinationAsync(TimeSpan.FromSeconds(5));

        _logger.LogInformation("Synthesizing TTS: {Text}", text);
        var pcm = _ttsService.SynthesizePcm(text);
        var pcmBytes = PcmToBytes(pcm);

        // Set up DTMF reader BEFORE playback — zero race window
        using var promptCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_callCts.Token, promptCts.Token);

        var dtmfTask = _dtmfChannel.Reader.ReadAsync(linkedCts.Token).AsTask();
        var playbackTask =
            _audioExtras.SendAudioFromStream(new MemoryStream(pcmBytes), AudioSamplingRatesEnum.Rate8KHz);

        var completed = await Task.WhenAny(dtmfTask, playbackTask);

        if (completed == dtmfTask && !dtmfTask.IsFaulted)
        {
            _audioExtras.CancelSendAudioFromStream();
            return DtmfResult.FromDigit(await dtmfTask);
        }

        promptCts.CancelAfter(timeout);
        try
        {
            var digit = await _dtmfChannel.Reader.ReadAsync(linkedCts.Token).AsTask();
            return DtmfResult.FromDigit(digit);
        }
        catch (OperationCanceledException)
        {
            return DtmfResult.Timeout;
        }
    }

    public async Task<DtmfResult> ReadPromptSegmentsAsync(IEnumerable<string> segments, TimeSpan timeout)
    {
        await WaitForAudioDestinationAsync(TimeSpan.FromSeconds(5));

        using var promptCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_callCts.Token, promptCts.Token);

        var dtmfTask = _dtmfChannel.Reader.ReadAsync(linkedCts.Token).AsTask();

        foreach (var text in segments)
        {
            _logger.LogInformation("Synthesizing TTS: {Text}", text);
            var pcm = _ttsService.SynthesizePcm(text);
            var pcmBytes = PcmToBytes(pcm);

            var playbackTask =
                _audioExtras.SendAudioFromStream(new MemoryStream(pcmBytes), AudioSamplingRatesEnum.Rate8KHz);
            var completed = await Task.WhenAny(dtmfTask, playbackTask);

            if (completed == dtmfTask && !dtmfTask.IsFaulted)
            {
                _audioExtras.CancelSendAudioFromStream();
                return DtmfResult.FromDigit(await dtmfTask);
            }

            await playbackTask;
        }

        promptCts.CancelAfter(timeout);
        try
        {
            var digit = await dtmfTask;
            return DtmfResult.FromDigit(digit);
        }
        catch (OperationCanceledException)
        {
            return DtmfResult.Timeout;
        }
    }

    public async Task<DtmfResult> WaitForDtmfAsync(TimeSpan timeout)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_callCts.Token);
        cts.CancelAfter(timeout);
        try
        {
            var digit = await _dtmfChannel.Reader.ReadAsync(cts.Token).AsTask();
            return DtmfResult.FromDigit(digit);
        }
        catch (OperationCanceledException)
        {
            return DtmfResult.Timeout;
        }
    }

    public async Task PlayDtmfAsync(string digits)
    {
        await WaitForAudioDestinationAsync(TimeSpan.FromSeconds(5));

        var tones = DtmfToneGenerator.GenerateSequence(digits, 8000, 200, 100);
        var toneBytes = PcmToBytes(tones);

        await _audioExtras.SendAudioFromStream(new MemoryStream(toneBytes), AudioSamplingRatesEnum.Rate8KHz);
    }

    public async Task SpeakAsync(string text)
    {
        await WaitForAudioDestinationAsync(TimeSpan.FromSeconds(5));

        _logger.LogInformation("Synthesizing TTS: {Text}", text);
        var pcm = _ttsService.SynthesizePcm(text);
        var pcmBytes = PcmToBytes(pcm);

        await _audioExtras.SendAudioFromStream(new MemoryStream(pcmBytes), AudioSamplingRatesEnum.Rate8KHz);
    }

    public async Task<string> CollectDigitsAsync(char terminator, TimeSpan timeout)
    {
        var digits = new StringBuilder();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_callCts.Token);
        cts.CancelAfter(timeout);

        try
        {
            while (await _dtmfChannel.Reader.WaitToReadAsync(cts.Token))
            while (_dtmfChannel.Reader.TryRead(out var digit))
            {
                if (digit == terminator) return digits.ToString();
                if (char.IsDigit(digit)) digits.Append(digit);
            }
        }
        catch (OperationCanceledException)
        {
        }

        return digits.ToString();
    }

    public async Task HangupAsync()
    {
        _logger.LogInformation("Hangup via SIPUserAgent");
        _sipUserAgent.Hangup();
        _callCts.Cancel();
        await Task.CompletedTask;
    }

    public void DrainPendingDtmf()
    {
        while (_dtmfChannel.Reader.TryRead(out _))
        {
        }
    }

    public async Task SendRawAudioAsync(Stream pcmStream)
    {
        await WaitForAudioDestinationAsync(TimeSpan.FromSeconds(5));
        var bytes = pcmStream.CanSeek ? pcmStream.Length : -1;
        var durationSec = bytes > 0 ? (double)bytes / (8000 * 2) : -1;
        _logger.LogInformation("Sending raw audio: {Bytes:N0} bytes, {Duration:F1}s", bytes, Math.Max(0, durationSec));
        await _audioExtras.SendAudioFromStream(pcmStream, AudioSamplingRatesEnum.Rate8KHz);
    }

    public async Task PlayWavAsync(string path)
    {
        await WaitForAudioDestinationAsync(TimeSpan.FromSeconds(5));
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        var wavStream = StripWavHeader(fs);
        await _audioExtras.SendAudioFromStream(wavStream, AudioSamplingRatesEnum.Rate8KHz);
    }

    public async Task PlayBitcrushedPcmAsync(short[] samples, int bits)
    {
        await WaitForAudioDestinationAsync(TimeSpan.FromSeconds(5));
        var crushed = BitCrush(samples, bits);
        var bytes = PcmToBytes(crushed);
        await _audioExtras.SendAudioFromStream(new MemoryStream(bytes), AudioSamplingRatesEnum.Rate8KHz);
    }

    private void OnCallHungup(SIPDialogue dialogue)
    {
        _logger.LogInformation("Remote hangup detected");
        _callCts.Cancel();
    }

    private void OnRtpEvent(IPEndPoint? src, RTPEvent rtpEvent, RTPHeader header)
    {
        if (header.PayloadType != 101 || !rtpEvent.EndOfEvent) return;

        var digit = DecodeDtmfEvent(rtpEvent.EventID);
        if (!digit.HasValue) return;

        lock (_debounceLock)
        {
            if (digit.Value == _lastDtmfDigit && (DateTime.UtcNow - _lastDtmfTime).TotalMilliseconds < 300)
                return;
            _lastDtmfDigit = digit.Value;
            _lastDtmfTime = DateTime.UtcNow;
        }

        _logger?.LogDebug("DTMF digit received: {Digit}", digit.Value);
        _dtmfChannel.Writer.TryWrite(digit.Value);
    }

    private char? DecodeDtmfEvent(byte eventID)
        {
            if (eventID >= 0 && eventID <= 9) return (char)('0' + eventID);
            if (eventID == 0x0A) return '*';
            if (eventID == 0x0B) return '#';
            if (eventID >= 0x0C && eventID <= 0x0F) return (char)('A' + (eventID - 0x0C));
            return null;
        }

    private static short[] BitCrush(short[] samples, int bits)
    {
        if (bits >= 16) return samples;
        var levels = 1 << bits;
        double maxLevel = levels - 1;
        var result = new short[samples.Length];
        for (var i = 0; i < samples.Length; i++)
        {
            var normalized = (samples[i] + 32768.0) / 65536.0;
            var level = (int)Math.Round(normalized * maxLevel);
            level = Math.Clamp(level, 0, (int)maxLevel);
            var denormalized = level / maxLevel * 65534.0 - 32767.0;
            result[i] = (short)denormalized;
        }

        return result;
    }

    private Stream StripWavHeader(Stream wavFile)
    {
        var reader = new BinaryReader(wavFile, Encoding.ASCII, true);
        var chunkId = reader.ReadBytes(4);
        if (Encoding.ASCII.GetString(chunkId) != "RIFF") return wavFile;
        reader.ReadInt32(); // size
        var format = reader.ReadBytes(4);
        if (Encoding.ASCII.GetString(format) != "WAVE") return wavFile;

        while (wavFile.Position < wavFile.Length)
        {
            var idBytes = reader.ReadBytes(4);
            if (idBytes.Length < 4) break;
            var id = Encoding.ASCII.GetString(idBytes);
            var size = reader.ReadInt32();
            if (id == "data") break;
            wavFile.Seek(size, SeekOrigin.Current);
        }

        return wavFile;
    }

    private async Task WaitForAudioDestinationAsync(TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (!cts.Token.IsCancellationRequested)
        {
            if (_rtpSession.AudioDestinationEndPoint != null) return;
            await Task.Delay(50, cts.Token);
        }
    }

    private byte[] PcmToBytes(short[] samples)
    {
        var bytes = new byte[samples.Length * 2];
        for (var i = 0; i < samples.Length; i++)
        {
            bytes[i * 2] = (byte)(samples[i] & 0xFF);
            bytes[i * 2 + 1] = (byte)((samples[i] >> 8) & 0xFF);
        }

        return bytes;
    }
}