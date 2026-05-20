using HotlineValetudo.Services;

namespace HotlineValetudo.Flows;

public interface IIvrFlow
{
    ILoggerFactory? LoggerFactory { get; set; }
    Task<bool> RunAsync(ICallSession session);
}