namespace Soenneker.Utils.MemoryStream.Tests;

internal class InvalidPositionStream : System.IO.MemoryStream
{
    private readonly long _invalidPosition;

    public InvalidPositionStream(byte[] buffer, long invalidPosition) : base(buffer)
    {
        _invalidPosition = invalidPosition;
    }

    public override long Position
    {
        get => _invalidPosition;
        set { } // Ignore set attempts
    }

    public override bool CanSeek => true;
}