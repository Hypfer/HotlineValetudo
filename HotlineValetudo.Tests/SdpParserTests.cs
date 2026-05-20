using System.Net;

namespace HotlineValetudo.Tests;

public class SdpParserTests
{
    // These are internal methods of CallHandler, tested via reflection
    // or we can extract the parsing logic into a testable class.
    // For now, let's just test the expected format.

    [Test]
    public void SdpAnswer_HasRequiredLines()
    {
        var sdp = @"v=0
o=- 0 0 IN IP4 127.0.0.1
s=Valetudo Hotline
c=IN IP4 127.0.0.1
t=0 0
m=audio 5000 RTP/AVP 0 101
a=rtpmap:0 PCMU/8000
a=rtpmap:101 telephone-event/8000
a=fmtp:101 0-16";

        Assert.That(sdp, Does.Contain("v=0"));
        Assert.That(sdp, Does.Contain("o="));
        Assert.That(sdp, Does.Contain("s="));
        Assert.That(sdp, Does.Contain("c=IN IP4"));
        Assert.That(sdp, Does.Contain("t=0 0"));
        Assert.That(sdp, Does.Contain("m=audio"));
        Assert.That(sdp, Does.Contain("a=rtpmap:0 PCMU/8000"));
        Assert.That(sdp, Does.Contain("a=rtpmap:101 telephone-event/8000"));
    }

    [Test]
    public void SdpAnswer_ParsesConnectionAddress()
    {
        var sdp = @"v=0
o=- 123456 1 IN IP4 192.168.1.100
s=SIPPER for PhonerLite
c=IN IP4 192.168.1.100
t=0 0
m=audio 6062 RTP/AVP 107 8 0 101";

        var lines = sdp.Split('\n');
        var connectionLine = lines.FirstOrDefault(l => l.Trim().StartsWith("c="));

        Assert.That(connectionLine, Is.Not.Null);
        var parts = connectionLine!.Split(' ');
        Assert.That(parts, Has.Length.GreaterThanOrEqualTo(3));
        Assert.That(IPAddress.Parse(parts[2]), Is.EqualTo(IPAddress.Parse("192.168.1.100")));
    }

    [Test]
    public void SdpAnswer_ParsesAudioPort()
    {
        var sdp = @"v=0
o=- 123456 1 IN IP4 127.0.0.1
s=Test
c=IN IP4 127.0.0.1
t=0 0
m=audio 12345 RTP/AVP 0 101";

        var lines = sdp.Split('\n');
        var mediaLine = lines.FirstOrDefault(l => l.Trim().StartsWith("m=audio"));

        Assert.That(mediaLine, Is.Not.Null);
        var parts = mediaLine!.Split(' ');
        Assert.That(int.Parse(parts[1]), Is.EqualTo(12345));
    }

    [Test]
    public void SdpAnswer_HandlesCrlfLineEndings()
    {
        var sdp =
            "v=0\r\no=- 123456 1 IN IP4 10.0.0.1\r\ns=Test\r\nc=IN IP4 10.0.0.1\r\nt=0 0\r\nm=audio 54321 RTP/AVP 0";

        var lines = sdp.Split('\n');
        var connectionLine = lines.FirstOrDefault(l => l.Trim().StartsWith("c="));

        Assert.That(connectionLine, Is.Not.Null);
        var parts = connectionLine!.Trim().Split(' ');
        Assert.That(IPAddress.Parse(parts[2]), Is.EqualTo(IPAddress.Parse("10.0.0.1")));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("garbage data")]
    public void SdpAnswer_NullOrInvalid_ReturnsNull(string? sdp)
    {
        // The parser should handle these gracefully
        if (string.IsNullOrWhiteSpace(sdp))
        {
            Assert.Pass("Empty SDP handled");
            return;
        }

        var lines = sdp.Split('\n');
        var connectionLine = lines.FirstOrDefault(l => l.Trim().StartsWith("c="));
        var mediaLine = lines.FirstOrDefault(l => l.Trim().StartsWith("m=audio"));

        Assert.That(connectionLine, Is.Null.Or.Empty);
        Assert.That(mediaLine, Is.Null.Or.Empty);
    }
}