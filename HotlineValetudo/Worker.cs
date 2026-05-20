using SIPSorcery.SIP;
using HotlineValetudo.Flows;
using HotlineValetudo.NumberStation;
using HotlineValetudo.Services;

namespace HotlineValetudo;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IIvrFlow _mainFlow;
    private readonly CallSessionManager _sessionManager;
    private readonly SipService _sipService;
    private readonly TtsService _ttsService;

    public Worker(ILogger<Worker> logger, ILoggerFactory loggerFactory, SipService sipService, TtsService ttsService,
        CallSessionManager sessionManager)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _sipService = sipService;
        _ttsService = ttsService;
        _sessionManager = sessionManager;

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("HotlineValetudo/1.0");

        _mainFlow = new MainMenuFlow(
            "Thank you for calling Valetudo Premium Customer Support. We'll be with you shortly.",
            [
                new MainMenuFlow.MenuItem(1, "To check for the latest Valetudo version, press 1.", new VersionCheckFlow(httpClient)),
                new MainMenuFlow.MenuItem(2, "To file a ticket, press 2.", new TicketFlow()),
                new MainMenuFlow.MenuItem(3, "To take a number, press 3.", new TakeANumberFlow()),
                new MainMenuFlow.MenuItem(4, "To speak to an expert, press 4.", new HoldMusicFlow()),
                new MainMenuFlow.MenuItem(5, "To receive a mystery transmission, press 5.", new SendMysteryTransmissionFlow()),

                new MainMenuFlow.MenuItem(0, "And to be connected with an operator, press 0.", new ArithmeticOperatorFlow())
            ]);
        WireLoggerFactory(_mainFlow);

        _sipService.OnCallAnswered += (uas, media) =>
        {
            var session = _sessionManager.TryCreateSingleSession(uas, media);
            if (session == null)
            {
                _logger.LogWarning("Line busy, sending 486");
                uas.Reject(SIPResponseStatusCodesEnum.BusyHere, "Line busy");
                return;
            }

            HandleCall(session);
        };
    }

    private void WireLoggerFactory(IIvrFlow flow)
    {
        flow.LoggerFactory = _loggerFactory;
    }

    public override Task StartAsync(CancellationToken stoppingToken)
    {
        _sipService.Start();
        return Task.CompletedTask;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Valetudo Hotline is running. Press Ctrl+C to shut down.");
        stoppingToken.Register(() => _sipService.Dispose());
        stoppingToken.Register(() => _ttsService.Dispose());
        return Task.CompletedTask;
    }

    private async void HandleCall(CallSession session)
    {
        _logger.LogInformation("Call answered from {From}", session.Id);
        try
        {
            await _mainFlow.RunAsync(session);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Call session {Id} canceled", session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in call session {Id}", session.Id);
        }
        finally
        {
            await session.DisposeAsync();
            _sessionManager.RemoveSession(session.Id);
        }
    }
}