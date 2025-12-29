using System;
using System.IO;

namespace Soenneker.Utils.MemoryStream.Tests;

internal class NonSeekableStream : Stream
{
    private readonly Stream _baseStream;

    public NonSeekableStream(Stream baseStream)
    {
        _baseStream = baseStream;
    }

    public override bool CanRead => _baseStream.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => _baseStream.CanWrite;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => _baseStream.Position;
        set => throw new NotSupportedException();
    }

    public override void Flush() => _baseStream.Flush();
    public override int Read(byte[] buffer, int offset, int count) => _baseStream.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => _baseStream.Write(buffer, offset, count);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _baseStream.Dispose();
        }
        base.Dispose(disposing);
    }
}

