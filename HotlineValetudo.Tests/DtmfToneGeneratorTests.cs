using HotlineValetudo.Audio;

namespace HotlineValetudo.Tests;

public class DtmfToneGeneratorTests
{
    [Test]
    public void GenerateTone_ProducesCorrectLength()
    {
        var samples = DtmfToneGenerator.GenerateTone('1', 8000, 100);
        Assert.That(samples, Has.Length.EqualTo(800));
    }

    [Test]
    public void GenerateTone_SamplesAreNonZero()
    {
        var samples = DtmfToneGenerator.GenerateTone('5', 8000, 100);
        var nonZero = samples.Count(s => s != 0);
        Assert.That(nonZero, Is.GreaterThan(700));
    }

    [Test]
    public void GenerateTone_SamplesWithinRange()
    {
        var samples = DtmfToneGenerator.GenerateTone('*', 8000, 100);
        foreach (var s in samples)
        {
            Assert.That(s, Is.GreaterThanOrEqualTo(short.MinValue));
            Assert.That(s, Is.LessThanOrEqualTo(short.MaxValue));
        }
    }

    [TestCase("1", 8000, 100, 50, 1200)]
    [TestCase("1337", 8000, 100, 50, 4800)]
    public void GenerateSequence_CorrectLength(string digits, int rate, int duration, int gap, int expected)
    {
        var samples = DtmfToneGenerator.GenerateSequence(digits, rate, duration, gap);
        Assert.That(samples, Has.Length.EqualTo(expected));
    }

    [Test]
    public void GenerateSequence_DifferentTonesHaveDifferentPatterns()
    {
        var tone1 = DtmfToneGenerator.GenerateTone('1', 8000, 100);
        var tone9 = DtmfToneGenerator.GenerateTone('9', 8000, 100);

        var different = 0;
        for (var i = 0; i < tone1.Length; i++)
            if (Math.Abs(tone1[i] - tone9[i]) > 1000)
                different++;

        Assert.That(different, Is.GreaterThan(500), "Different DTMF keys should produce different waveforms");
    }
}