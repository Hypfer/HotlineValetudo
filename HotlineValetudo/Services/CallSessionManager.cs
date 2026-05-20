using System.Collections.Concurrent;
using SIPSorcery.SIP.App;

namespace HotlineValetudo.Services;

public class CallSessionManager(
    SipService sipService,
    TtsService ttsService,
    ILogger<CallSessionManager> logger,
    ILoggerFactory loggerFactory)
{
    private readonly ConcurrentDictionary<Guid, CallSession> _sessions = new();

    public CallSession? TryCreateSingleSession(SIPServerUserAgent uas, IMediaSession media)
    {
        if (_sessions.Values.Any(s => s.IsActive))
        {
            logger.LogWarning("Call rejected: another call is active");
            return null;
        }

        var sessionLogger = loggerFactory.CreateLogger<CallSession>();
        var session = new CallSession(sipService.UserAgent, uas, media, ttsService, sessionLogger);
        _sessions[session.Id] = session;
        logger.LogInformation("Call session created: {Id}", session.Id);
        return session;
    }

    public void RemoveSession(Guid id)
    {
        _sessions.TryRemove(id, out var session);
        logger.LogInformation("Call session removed: {Id}", id);
    }
}