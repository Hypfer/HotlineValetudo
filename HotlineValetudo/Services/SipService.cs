using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using SIPSorcery.Media;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using HotlineValetudo.Config;

namespace HotlineValetudo.Services;

public class SipService : IDisposable
{
    private readonly ILogger<SipService> _logger;
    private readonly SipOptions _options;
    private SIPRegistrationUserAgent? _registrationAgent;
    private bool _disposed;
    private Timer? _keepAliveTimer;

    public SipService(IOptions<SipOptions> options, ILogger<SipService> logger)
    {
        _options = options.Value;
        _logger = logger;

        Transport = new SIPTransport();
        Transport.ResolveSIPUriCallbackAsync = ResolveSipEndPointAsync;

        Transport.SIPRequestInTraceEvent += (localPort, remotePort, req) =>
        {
            _logger.LogTrace("RAW SIP REQUEST IN:\n{req}", req.ToString());
        };
        Transport.SIPResponseInTraceEvent += (localPort, remotePort, resp) =>
        {
            _logger.LogTrace("RAW SIP RESPONSE IN:\n{resp}", resp.ToString());
        };
        Transport.SIPBadRequestInTraceEvent += (localPort, remotePort, msg, error, raw) =>
        {
            _logger.LogDebug("BAD SIP REQUEST: {msg} {error}", msg, error);
        };
        Transport.SIPBadResponseInTraceEvent += (localPort, remotePort, msg, error, raw) =>
        {
            _logger.LogDebug("BAD SIP RESPONSE: {msg} {error}", msg, error);
        };

        var listenPort = _options.Mode == "client"
            ? ExtractPortFromSipUri(_options.ClientMode.LocalSipUri)
            : _options.ServerMode.ListenPort;
            
        var listenIp = _options.Mode == "client" 
            ? "0.0.0.0" 
            : "0.0.0.0";
        var parsedListenIp = IPAddress.TryParse(listenIp, out var ip) ? ip : IPAddress.Any;

        var udpChannel = new SIPUDPChannel(new IPEndPoint(parsedListenIp, listenPort));
        Transport.AddSIPChannel(udpChannel);

        UserAgent = new SIPUserAgent(Transport, null, true);
        UserAgent.OnIncomingCall += OnIncomingCall;

        if (_options.Mode == "client")
        {
            var registrarHost = ExtractHostFromSipUri(_options.ClientMode.RegistrarUri);
            var registrarPort = ExtractPortFromSipUri(_options.ClientMode.RegistrarUri);
            var registrarServer = $"{registrarHost}:{registrarPort}";

            _logger.LogInformation("Configuring SIP client: registrar {Registrar}", registrarServer);

            var sipUri = SIPURI.ParseSIPURI(_options.ClientMode.LocalSipUri);
            var sipAccountAor = SIPURI.ParseSIPURI(_options.ClientMode.RegistrarUri);
            sipAccountAor.User = _options.ClientMode.Username;

            _registrationAgent = new SIPRegistrationUserAgent(
                Transport,
                null,
                sipAccountAor,
                _options.ClientMode.Username,
                _options.ClientMode.Password,
                null,
                registrarHost,
                sipUri,
                120,
                null);

            _registrationAgent.RegistrationSuccessful += (uri, resp) =>
            {
                _logger.LogInformation("SIP registration successful for {Uri}", uri);
                StartKeepAlives(registrarHost, registrarPort);
            };

            _registrationAgent.RegistrationFailed += (uri, resp, error) =>
            {
                _logger.LogError("SIP registration failed for {Uri}: {Error}", uri, error);
                StopKeepAlives();
            };
                
            _registrationAgent.RegistrationTemporaryFailure += (uri, resp, error) =>
                _logger.LogWarning("SIP registration temporary failure for {Uri}: {Error}", uri, error);
            _registrationAgent.RegistrationRemoved += (uri, resp) =>
            {
                _logger.LogWarning("SIP registration removed for {Uri}", uri);
                StopKeepAlives();
            };
        }
    }

    private static string ExtractHostFromSipUri(string sipUri)
    {
        var uri = SIPURI.ParseSIPURI(sipUri);
        return uri.HostAddress;
    }

    private static int ExtractPortFromSipUri(string sipUri)
    {
        var uri = SIPURI.ParseSIPURI(sipUri);
        return int.TryParse(uri.HostPort, out var port) ? port : 5060;
    }

    public SIPTransport Transport { get; }

    public SIPUserAgent UserAgent { get; }

    public void Dispose()
    {
        if (_disposed) return;
        StopKeepAlives();
        _registrationAgent?.Stop();
        Transport.Shutdown();
        _disposed = true;
    }

    public event Action<SIPServerUserAgent, IMediaSession>? OnCallAnswered;

    public void Start()
    {
        var channels = Transport.GetSIPChannels();
        _logger.LogInformation("SIP transport listening on {Count} channel(s) (mode: {Mode})",
            channels.Count, _options.Mode);

        if (_registrationAgent != null)
        {
            _registrationAgent.Start();
            _logger.LogInformation("SIP client registration started");
        }
    }

    private void StartKeepAlives(string host, int port)
    {
        if (_keepAliveTimer != null) return;
        
        var sipUri = SIPURI.ParseSIPURI($"sip:{host}:{port}");
        
        // Send a valid SIP OPTIONS keep-alive every 25 seconds
        // This is safe and prevents the FritzBox from replying with ICMP Port Unreachable,
        // which would cause the socket to block the PBX IP entirely.
        _keepAliveTimer = new Timer(async state =>
        {
            try
            {
                var optionsReq = SIPRequest.GetRequest(
                    SIPMethodsEnum.OPTIONS, 
                    sipUri, 
                    new SIPToHeader(null, sipUri, null), 
                    new SIPFromHeader(null, SIPURI.ParseSIPURI(_options.ClientMode.LocalSipUri), CallProperties.CreateNewTag()));
                    
                optionsReq.Header.CallId = CallProperties.CreateNewCallId();
                optionsReq.Header.CSeq = 1;
                
                await Transport.SendRequestAsync(optionsReq);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Failed to send SIP OPTIONS keep-alive");
            }
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(25));
    }

    private void StopKeepAlives()
    {
        _keepAliveTimer?.Dispose();
        _keepAliveTimer = null;
    }

    private async void OnIncomingCall(SIPUserAgent userAgent, SIPRequest request)
    {
        if (request.Method != SIPMethodsEnum.INVITE) return;

        if (userAgent.IsCallActive)
        {
            _logger.LogInformation("Rejecting second incoming call from {From} because we are already in an active call", request.Header.From.FromURI);
            var busyUas = new SIPServerUserAgent(Transport, null, new UASInviteTransaction(Transport, request, null), null);
            busyUas.Progress(SIPResponseStatusCodesEnum.Trying, null, null, null, null);
            busyUas.Reject(SIPResponseStatusCodesEnum.BusyHere, null);
            return;
        }

        if (userAgent.IsRinging || userAgent.IsCalling)
        {
            _logger.LogInformation("Another call is currently ringing. Ignoring this INVITE from {From} to prevent race conditions with PBX parallel ringing.", request.Header.From.FromURI);
            // We just let it ring. The PBX will cancel it automatically once we finish answering the first call.
            return;
        }

        _logger.LogInformation("Incoming INVITE from {Remote}", request.RemoteSIPEndPoint);

        var uas = userAgent.AcceptCall(request);

        var localIp = IPAddress.Parse(GetLocalIp());
        var mediaSession = new VoIPMediaSession();
        mediaSession.AudioExtrasSource.SetSource(AudioSourcesEnum.None);

        try
        {
            var answerOk = await userAgent.Answer(uas, mediaSession, localIp);
            _logger.LogInformation("Answer result: {Ok}, HasAudio: {Audio}, Dest: {Dest}",
                answerOk, mediaSession.HasAudio,
                mediaSession.GetType().GetProperty("AudioDestinationEndPoint")?.GetValue(mediaSession));

            if (answerOk) OnCallAnswered?.Invoke(uas, mediaSession);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Answer threw exception");
        }
    }

    private string GetLocalIp()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return ip.ToString();
        return "127.0.0.1";
    }

    private Task<SIPEndPoint?> ResolveSipEndPointAsync(SIPURI uri, bool preferIPv6, CancellationToken ct)
    {
        try
        {
            var ip = IPAddress.Parse(uri.HostAddress);
            var port = int.TryParse(uri.HostPort, out var p) ? p : 5060;
            _logger.LogDebug("Resolved {Uri} to {Ip}:{Port}", uri, ip, port);
            return Task.FromResult<SIPEndPoint?>(new SIPEndPoint(SIPProtocolsEnum.udp, ip, port));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve SIP URI {Uri}", uri);
            return Task.FromResult<SIPEndPoint?>(null);
        }
    }
}
