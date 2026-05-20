using HotlineValetudo.Services;

namespace HotlineValetudo.Flows;

public class TicketFlow : IIvrFlow
{
    private static readonly string[] StaticMessages =
    {
        "no endpoints available for service",
        "Readiness probe failed: HTTP probe failed with statuscode: 503",
        "connection refused",
        "AADSTS50011: The reply URL specified in the request does not match the reply URLs configured for the application",
        "ORA-00060: deadlock detected while waiting for resource",
        "ORA-12154: TNS:could not resolve the connect identifier specified",
        "ORA-01536: space quota exceeded for tablespace",
        "java.lang.OutOfMemoryError: PermGen space",
        "java.lang.OutOfMemoryError: Java heap space",
        "The model is currently overloaded. Please try again later",
        "tool use concurrency issues",
        "Rate limit reached for requests. Please try again in 20 milliseconds",
        "insufficient_quota. You exceeded your current quota, please check your plan and billing details",
        "LDAP error code 49: Invalid Credentials",
        "context deadline exceeded"
    };

    private static readonly Random Rng = new();
    public ILoggerFactory? LoggerFactory { get; set; }

    public async Task<bool> RunAsync(ICallSession session)
    {
        await session.SpeakAsync("Please hold while we transfer your call.");
        await Task.Delay(5000, session.CallCancellationToken);

        var message = PickMessage();
        await session.SpeakAsync("Error.");
        await session.SpeakAsync(message);
        await Task.Delay(400, session.CallCancellationToken);
        await session.HangupAsync();

        return false;
    }

    private static string PickNodeMessage()
    {
        var nodes = Rng.Next(1, 10);
        var kind = Rng.Next(2) switch
        {
            0 => "Insufficient cpu",
            1 => "Insufficient memory",
            _ => "Insufficient cpu"
        };
        return $"0/{nodes} nodes are available: {nodes} {kind}";
    }

    private static string PickMessage()
    {
        return Rng.Next(6) == 0 ? PickNodeMessage() : StaticMessages[Rng.Next(StaticMessages.Length)];
    }
}