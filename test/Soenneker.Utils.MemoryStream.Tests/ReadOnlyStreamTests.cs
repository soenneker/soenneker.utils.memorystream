using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Soenneker.Tests.HostedUnit;
using Soenneker.Utils.MemoryStream.Abstract;

namespace Soenneker.Utils.MemoryStream.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class ReadOnlyStreamTests : HostedUnitTest
{
    private readonly IMemoryStreamUtil _util;

    public ReadOnlyStreamTests(Host host) : base(host)
    {
        _util = Resolve<IMemoryStreamUtil>(true);
    }

    [Test]
    public async Task Bytes_WrapsOnlyTheRequestedSlice(CancellationToken cancellationToken)
    {
        byte[] bytes = [0, 1, 2, 3, 4];
        using Stream stream = _util.GetReadOnly(bytes.AsMemory(1, 3));
        stream.CanWrite.Should().BeFalse();
        stream.Length.Should().Be(3);
        stream.Position.Should().Be(0);
        using var destination = new System.IO.MemoryStream();
        await stream.CopyToAsync(destination, cancellationToken);
        destination.ToArray().Should().Equal(1, 2, 3);
        bytes.Should().Equal(0, 1, 2, 3, 4);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Text_EncodesUnicodeAcrossSmallReads(bool useMemory, CancellationToken cancellationToken)
    {
        const string text = "Hello café 世界 🐈\n";
        using Stream stream = useMemory ? _util.GetReadOnly(("!" + text + "!").AsMemory(1, text.Length)) : _util.GetReadOnly(text);
        stream.CanWrite.Should().BeFalse();
        using var destination = new System.IO.MemoryStream();
        byte[] buffer = new byte[1];
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken)) != 0)
            destination.Write(buffer, 0, read);
        destination.ToArray().Should().Equal(Encoding.UTF8.GetBytes(text));
    }

    [Test]
    public void EmptyInputs_AreImmediatelyAtEnd()
    {
        using Stream bytes = _util.GetReadOnly(ReadOnlyMemory<byte>.Empty);
        using Stream chars = _util.GetReadOnly(ReadOnlyMemory<char>.Empty);
        using Stream text = _util.GetReadOnly(string.Empty);
        bytes.ReadByte().Should().Be(-1);
        chars.ReadByte().Should().Be(-1);
        text.ReadByte().Should().Be(-1);
    }

    [Test]
    public void NullString_IsRejected()
    {
        Action create = () => _util.GetReadOnly((string)null!);
        create.Should().Throw<ArgumentNullException>();
    }
}
