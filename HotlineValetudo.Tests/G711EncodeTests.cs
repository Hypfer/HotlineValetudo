using HotlineValetudo.Audio;

namespace HotlineValetudo.Tests;

public class G711EncodeTests
{
    [TestCase(0, 0xFF)]
    [TestCase(1, 0xFF)]
    [TestCase(-1, 0x7F)]
    [TestCase(32635, 0x80)]
    [TestCase(-32635, 0x00)]
    [TestCase(16383, 0x8F)]
    [TestCase(-16384, 0x0F)]
    [TestCase(8191, 0x9F)]
    [TestCase(4095, 0xAF)]
    [TestCase(2047, 0xBE)]
    [TestCase(1023, 0xCD)]
    [TestCase(511, 0xDB)]
    [TestCase(255, 0xE7)]
    [TestCase(127, 0xEF)]
    public void PcmuEncode_SingleSamples(short sample, byte expected)
    {
        var result = G711Encode.PcmuEncode([sample]);
        Assert.That(result, Has.Length.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(expected));
    }

    [Test]
    public void PcmuEncode_Silence_Is0xFF()
    {
        var silence = new short[160];
        var result = G711Encode.PcmuEncode(silence);
        Assert.That(result, Has.Length.EqualTo(160));
        foreach (var b in result)
            Assert.That(b, Is.EqualTo(0xFF));
    }

    [Test]
    public void PcmuEncode_PreservesLength()
    {
        var samples = new short[1000];
        var result = G711Encode.PcmuEncode(samples);
        Assert.That(result, Has.Length.EqualTo(1000));
    }

    [Test]
    public void PcmuEncode_SineWave_ProducesValidBytes()
    {
        var samples = new short[8000];
        for (var i = 0; i < samples.Length; i++)
            samples[i] = (short)(16000 * Math.Sin(2 * Math.PI * 440 * i / 8000));

        var result = G711Encode.PcmuEncode(samples);
        Assert.That(result, Has.Length.EqualTo(8000));

        foreach (var b in result)
        {
            Assert.That(b, Is.GreaterThanOrEqualTo(0x00));
            Assert.That(b, Is.LessThanOrEqualTo(0xFF));
        }
    }
}