using HotlineValetudo.Services;

namespace HotlineValetudo.Flows;

public class SimpleResponseFlow(string response) : IIvrFlow
{
    public ILoggerFactory? LoggerFactory { get; set; }

    public async Task<bool> RunAsync(ICallSession session)
    {
        await session.SpeakAsync(response);
        return true;
    }
}