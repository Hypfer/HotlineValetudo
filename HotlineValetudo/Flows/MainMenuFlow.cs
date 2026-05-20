using HotlineValetudo.Services;

namespace HotlineValetudo.Flows;

public class MainMenuFlow(string welcome, IEnumerable<MainMenuFlow.MenuItem> items) : IIvrFlow
{
    private readonly IReadOnlyList<MenuItem> _items = items.ToList();

    private ILoggerFactory? _loggerFactory;

    public ILoggerFactory? LoggerFactory
    {
        get => _loggerFactory;
        set
        {
            _loggerFactory = value;
            foreach (var item in _items) item.Flow.LoggerFactory = value;
        }
    }

    public async Task<bool> RunAsync(ICallSession session)
    {
        var logger = _loggerFactory?.CreateLogger<MainMenuFlow>();

        // Play silence to let the remote side's audio path establish
        await session.SpeakAsync("");
        
        await session.SpeakAsync(welcome);
        // TODO: some kind of jingle
        await session.SpeakAsync("For quality and compliance purposes, this call may be recorded to systemdeee-journaldeee.");
        await Task.Delay(1000);

        //await session.SpeakAsync("You are in a maze of twisty little passages, all alike.");

        
        logger?.LogInformation("Welcome message done");

        session.DrainPendingDtmf();
        var timeouts = 0;
        const int maxTimeouts = 3;

        while (session.IsActive && timeouts < maxTimeouts)
        {
            if (timeouts > 0)
            {
                var retry = timeouts switch
                {
                    1 => "I didn't catch that.",
                    2 => "Are you still there?",
                    _ => "I'm going to hang up now."
                };
                await session.SpeakAsync(retry);
                logger?.LogInformation("Menu timeout {Timeout}/{MaxTimeouts}", timeouts, maxTimeouts);
            }

            var result = await session.ReadPromptSegmentsAsync(BuildMenuSegments(), TimeSpan.FromSeconds(30));

            if (result.IsTimeout)
            {
                timeouts++;
                if (timeouts >= maxTimeouts) break;
                continue;
            }

            timeouts = 0;
            logger?.LogInformation("DTMF received: {Digit}", result.Digit);

            if (result.Digit is >= 'A' and <= 'D')
            {
                await session.SpeakAsync($"You've found Easter Egg Placeholder {result.Digit}.");
                logger?.LogInformation("Easter egg triggered: {Digit}", result.Digit);
                continue;
            }

            var key = char.GetNumericValue(result.Digit);
            if (key < 0 || key > 9)
            {
                await session.SpeakAsync("Invalid selection. Please press a number.");
                continue;
            }

            var item = _items.FirstOrDefault(m => m.Key == (int)key);
            if (item == null)
            {
                await session.SpeakAsync("The selected option does not exist.");
                logger?.LogWarning("Unknown menu key {Key}", key);
                continue;
            }

            logger?.LogInformation("Menu selection: {Key} - {Prompt}", key, item.PromptText);
            await item.Flow.RunAsync(session);

            if (!session.IsActive) return false;
        }

        await session.HangupAsync();
        return false;
    }

    private IEnumerable<string> BuildMenuSegments()
    {
        yield return "This is the main menu.";
        foreach (var item in _items) yield return item.PromptText;
        yield return "Your selection please!";
    }


    public record MenuItem(int Key, string PromptText, IIvrFlow Flow);
}