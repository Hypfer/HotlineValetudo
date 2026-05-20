using HotlineValetudo.Services;

namespace HotlineValetudo.Flows;

public class ArithmeticOperatorFlow : IIvrFlow
{
    private static readonly (string Symbol, string Name, string FirstTerm, string SecondTerm, Func<long, long, long> Op)
        [] Operators =
        [
            ("+", "plus", "first operand", "second operand", (a, b) => a + b),
            ("-", "minus", "minuend", "subtrahend", (a, b) => a - b),
            ("*", "times", "multiplicand", "multiplier", (a, b) => a * b),
            ("/", "divided by", "dividend", "divisor", (a, b) => b == 0 ? long.MinValue : a / b),
            ("%", "modulo", "dividend", "divisor", (a, b) => b == 0 ? long.MinValue : a % b)
        ];

    public ILoggerFactory? LoggerFactory { get; set; }

    public async Task<bool> RunAsync(ICallSession session)
    {
        session.DrainPendingDtmf();

        var rng = new Random();
        var chosen = Operators[rng.Next(Operators.Length)];

        await session.SpeakAsync("You are now connected to the " + chosen.Name + " operator.");

        var num1 = await ReadConfirmedNumber(session, chosen.FirstTerm);
        if (!session.IsActive) return false;

        var num2 = await ReadConfirmedNumber(session, chosen.SecondTerm);
        if (!session.IsActive) return false;

        // Division/modulo by zero — dramatic hangup
        if ((chosen.Symbol == "/" || chosen.Symbol == "%") && num2 == 0)
        {
            await session.SpeakAsync(num1 + " " + chosen.Name + " " + num2 + " is     ");
            await session.HangupAsync();
            return false;
        }

        var result = chosen.Op(num1, num2);
        await session.SpeakAsync("The result of " + num1 + " " + chosen.Name + " " + num2 + " is " + result);
        await session.SpeakAsync("Thank you for using Valetudo basic arithmetic services.");
        await session.HangupAsync();
        return false;
    }

    private async Task<long> ReadConfirmedNumber(ICallSession session, string termName)
    {
        var retries = 0;
        const int maxRetries = 3;

        while (session.IsActive && retries < maxRetries)
        {
            session.DrainPendingDtmf();
            await session.SpeakAsync("Please specify your " + termName + " and end your input with the pound key.");
            var digits = await session.CollectDigitsAsync('#', TimeSpan.FromSeconds(15));

            if (!long.TryParse(digits, out var num))
            {
                retries++;
                if (retries >= maxRetries)
                {
                    await session.SpeakAsync("I couldn't understand that number. Goodbye.");
                    await session.HangupAsync();
                    throw new OperationCanceledException();
                }

                await session.SpeakAsync("I didn't catch that. Please try again.");
                continue;
            }

            await session.SpeakAsync("Your " + termName + " is " + digits + ". Press 1 to confirm, or 2 to re-enter.");
            var result = await session.WaitForDtmfAsync(TimeSpan.FromSeconds(10));

            if (result.IsTimeout)
            {
                retries++;
                if (retries >= maxRetries)
                {
                    await session.SpeakAsync("Goodbye.");
                    await session.HangupAsync();
                    throw new OperationCanceledException();
                }

                await session.SpeakAsync("I didn't catch that. Please try again.");
                continue;
            }

            if (result.Digit == '1') return num;
        }

        await session.SpeakAsync("Goodbye.");
        await session.HangupAsync();
        throw new OperationCanceledException();
    }
}