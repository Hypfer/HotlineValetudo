using HotlineValetudo.Buzzer;
using HotlineValetudo.Flows;
using HotlineValetudo.Services;

namespace HotlineValetudo.NumberStation;

public class TakeANumberFlow : IIvrFlow
{
    private readonly BuzzerFlow _buzzerFlow = new();
    private readonly Random _rng = new();
    private readonly NumberStationFlow _stationFlow = new();

    public ILoggerFactory? LoggerFactory
    {
        get => throw new NotSupportedException();
        set
        {
            _buzzerFlow.LoggerFactory = value;
            _stationFlow.LoggerFactory = value;
        }
    }

    public async Task<bool> RunAsync(ICallSession session)
    {
        var pick = _rng.Next(2);
        var flow = pick == 0 ? (IIvrFlow)_buzzerFlow : _stationFlow;

        await Task.Delay(1200);
        return await flow.RunAsync(session);
    }
}