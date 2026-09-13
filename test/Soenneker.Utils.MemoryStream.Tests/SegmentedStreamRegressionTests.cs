using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.IO;

namespace Soenneker.Utils.MemoryStream.Tests;

public class SegmentedStreamRegressionTests
{
    [Test]
    public async Task ByteSpanPreservesSegmentationAndPosition()
    {
        await using var util = new MemoryStreamUtil();
        byte[] input = new byte[500_000];
        new System.Random(1).NextBytes(input);
        using System.IO.MemoryStream stream = util.GetSync(input.AsSpan());
        stream.Position.Should().Be(0);
        stream.Length.Should().Be(input.Length);
        util.GetManagerSync().LargePoolInUseSize.Should().Be(0);
        stream.Position = 131_071;
        byte[] result = await util.GetBytesFromStream(stream, keepOpen: true);
        result.AsSpan().SequenceEqual(input.AsSpan(131_071)).Should().BeTrue();
        stream.Position.Should().Be(131_071);
        util.GetManagerSync().LargePoolInUseSize.Should().Be(0);
    }

    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(32767)]
    [Arguments(43689)]
    [Arguments(200_000)]
    public async Task CharactersRoundTripAcrossBuffers(int length)
    {
        await using var util = new MemoryStreamUtil();
        string input = new string('界', length) + "😀\uD800";
        byte[] expected = Encoding.UTF8.GetBytes(input);
        using System.IO.MemoryStream fromString = util.GetSync(input);
        using System.IO.MemoryStream fromSpan = util.GetSync(input.AsSpan());
        fromString.Position.Should().Be(0);
        fromSpan.Position.Should().Be(0);
        fromString.ToArray().Should().Equal(expected);
        fromSpan.ToArray().Should().Equal(expected);
        util.GetManagerSync().LargePoolInUseSize.Should().Be(0);
    }
}
