using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Microsoft.IO;
using Soenneker.Utils.AsyncSingleton;
using Soenneker.Utils.MemoryStream.Abstract;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.MemoryStream;

/// <inheritdoc cref="IMemoryStreamUtil"/>
public sealed class MemoryStreamUtil : IMemoryStreamUtil
{
    private static readonly Encoding _utf8 = Encoding.UTF8;

    private readonly AsyncSingleton<RecyclableMemoryStreamManager> _manager;

    public MemoryStreamUtil()
    {
        _manager = new AsyncSingleton<RecyclableMemoryStreamManager>(() => new RecyclableMemoryStreamManager());
    }

    public ValueTask<RecyclableMemoryStreamManager> GetManager(CancellationToken cancellationToken = default) => _manager.Get(cancellationToken);

    public RecyclableMemoryStreamManager GetManagerSync(CancellationToken cancellationToken = default) => _manager.GetSync(cancellationToken);

    public ValueTask<System.IO.MemoryStream> Get(CancellationToken cancellationToken = default)
    {
        ValueTask<RecyclableMemoryStreamManager> vt = GetManager(cancellationToken);

        if (vt.IsCompletedSuccessfully)
            return new ValueTask<System.IO.MemoryStream>(vt.Result.GetStream());

        return GetSlow(vt);

        static async ValueTask<System.IO.MemoryStream> GetSlow(ValueTask<RecyclableMemoryStreamManager> mgrTask)
        {
            RecyclableMemoryStreamManager mgr = await mgrTask.NoSync();
            return mgr.GetStream();
        }
    }

    public System.IO.MemoryStream GetSync(CancellationToken cancellationToken = default) =>
        GetManagerSync(cancellationToken)
            .GetStream();

    public ValueTask<System.IO.MemoryStream> Get(byte[] bytes, CancellationToken cancellationToken = default)
    {
        if (bytes is null)
            throw new ArgumentNullException(nameof(bytes));

        ValueTask<RecyclableMemoryStreamManager> vt = GetManager(cancellationToken);

        if (vt.IsCompletedSuccessfully)
            return new ValueTask<System.IO.MemoryStream>(vt.Result.GetStream(bytes));

        return GetBytesSlow(vt, bytes);

        static async ValueTask<System.IO.MemoryStream> GetBytesSlow(ValueTask<RecyclableMemoryStreamManager> mgrTask, byte[] b)
        {
            RecyclableMemoryStreamManager mgr = await mgrTask.NoSync();
            return mgr.GetStream(b);
        }
    }

    public System.IO.MemoryStream GetSync(byte[] bytes, CancellationToken cancellationToken = default)
    {
        if (bytes is null)
            throw new ArgumentNullException(nameof(bytes));

        return GetManagerSync(cancellationToken)
            .GetStream(bytes);
    }

    public ValueTask<System.IO.MemoryStream> Get(string str, CancellationToken cancellationToken = default)
    {
        if (str is null)
            throw new ArgumentNullException(nameof(str));

        ValueTask<RecyclableMemoryStreamManager> vt = GetManager(cancellationToken);

        if (vt.IsCompletedSuccessfully)
            return new ValueTask<System.IO.MemoryStream>(GetStreamFromString(vt.Result, str));

        return GetStringSlow(vt, str);

        static async ValueTask<System.IO.MemoryStream> GetStringSlow(ValueTask<RecyclableMemoryStreamManager> mgrTask, string s)
        {
            RecyclableMemoryStreamManager mgr = await mgrTask.NoSync();
            return GetStreamFromString(mgr, s);
        }
    }

    public System.IO.MemoryStream GetSync(string str, CancellationToken cancellationToken = default)
    {
        if (str is null)
            throw new ArgumentNullException(nameof(str));

        return GetStreamFromString(GetManagerSync(cancellationToken), str);
    }

    private static System.IO.MemoryStream GetStreamFromString(RecyclableMemoryStreamManager mgr, string str)
    {
        if (str.Length == 0)
            return mgr.GetStream();

        int byteCount = _utf8.GetByteCount(str);

        if (byteCount == 0)
            return mgr.GetStream();

        System.IO.MemoryStream ms = mgr.GetStream(tag: null, requiredSize: byteCount);

        try
        {
            ms.SetLength(byteCount);

            if (ms.TryGetBuffer(out ArraySegment<byte> seg))
            {
                _utf8.GetBytes(str.AsSpan(), seg.AsSpan(0, byteCount));
                ms.Position = 0;
                return ms;
            }

            byte[] tmp = GC.AllocateUninitializedArray<byte>(byteCount);
            _utf8.GetBytes(str.AsSpan(), tmp);
            ms.Write(tmp, 0, tmp.Length);
            ms.Position = 0;
            return ms;
        }
        catch
        {
            ms.Dispose();
            throw;
        }
    }

    public async ValueTask<byte[]> GetBytesFromStream(Stream stream, bool keepOpen = false, CancellationToken cancellationToken = default)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        try
        {
            // Fast path: MemoryStream (includes RecyclableMemoryStream since it derives from MemoryStream)
            if (stream is System.IO.MemoryStream memStream)
            {
                long pos64 = memStream.CanSeek ? memStream.Position : 0;
                long len64 = memStream.Length;

                if (pos64 < 0 || pos64 > len64)
                    throw new InvalidOperationException("Stream position is out of bounds.");

                int remaining = checked((int)(len64 - pos64));

                if (remaining == 0)
                    return [];

                if (memStream.TryGetBuffer(out ArraySegment<byte> seg))
                {
                    byte[] result = GC.AllocateUninitializedArray<byte>(remaining);

                    int srcOffset = checked(seg.Offset + (int)pos64);
                    Buffer.BlockCopy(seg.Array!, srcOffset, result, 0, remaining);

                    return result;
                }

                // Fallback (still position-aware)
                byte[] all = memStream.ToArray();

                if (pos64 == 0 && all.Length == remaining)
                    return all;

                byte[] slice = GC.AllocateUninitializedArray<byte>(remaining);
                Buffer.BlockCopy(all, (int)pos64, slice, 0, remaining);
                return slice;
            }

            // Non-MemoryStream path
            int initialSize = 0;

            if (stream.CanSeek)
            {
                long remaining = stream.Length - stream.Position;
                if (remaining > 0 && remaining <= int.MaxValue)
                    initialSize = (int)remaining;
            }

            RecyclableMemoryStreamManager mgr = await GetManager(cancellationToken)
                .NoSync();

            System.IO.MemoryStream buffer = initialSize > 0 ? mgr.GetStream(tag: null, requiredSize: initialSize) : mgr.GetStream();

            try
            {
                await stream.CopyToAsync(buffer, cancellationToken)
                            .NoSync();

                // Note: CopyToAsync writes from current stream.Position onward.
                // buffer.Position is at end; ToArray returns the whole buffer contents.
                return buffer.ToArray();
            }
            finally
            {
                await buffer.DisposeAsync()
                            .NoSync();
            }
        }
        finally
        {
            if (!keepOpen)
                await stream.DisposeAsync()
                            .NoSync();
        }
    }

    public System.IO.MemoryStream GetSync(ReadOnlySpan<byte> bytes, CancellationToken cancellationToken = default)
    {
        RecyclableMemoryStreamManager mgr = GetManagerSync(cancellationToken);

        if (bytes.Length == 0)
            return mgr.GetStream();

        System.IO.MemoryStream ms = mgr.GetStream(tag: null, requiredSize: bytes.Length);

        try
        {
            // IMPORTANT: TryGetBuffer exposes only [0..Length), not Capacity.
            ms.SetLength(bytes.Length);

            if (ms.TryGetBuffer(out ArraySegment<byte> seg))
            {
                bytes.CopyTo(seg.AsSpan(0, bytes.Length));
                ms.Position = 0;
                return ms;
            }

            ms.Write(bytes);
            ms.Position = 0;
            return ms;
        }
        catch
        {
            ms.Dispose();
            throw;
        }
    }

    public ValueTask<System.IO.MemoryStream> Get(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
    {
        ValueTask<RecyclableMemoryStreamManager> vt = GetManager(cancellationToken);

        if (vt.IsCompletedSuccessfully)
            return new ValueTask<System.IO.MemoryStream>(GetStreamFromBytes(vt.Result, bytes.Span));

        return GetBytesSlow(vt, bytes);

        static async ValueTask<System.IO.MemoryStream> GetBytesSlow(ValueTask<RecyclableMemoryStreamManager> mgrTask, ReadOnlyMemory<byte> b)
        {
            RecyclableMemoryStreamManager mgr = await mgrTask.NoSync();
            return GetStreamFromBytes(mgr, b.Span);
        }

        static System.IO.MemoryStream GetStreamFromBytes(RecyclableMemoryStreamManager mgr, ReadOnlySpan<byte> span)
        {
            if (span.Length == 0)
                return mgr.GetStream();

            System.IO.MemoryStream ms = mgr.GetStream(tag: null, requiredSize: span.Length);

            try
            {
                ms.SetLength(span.Length);

                if (ms.TryGetBuffer(out ArraySegment<byte> seg))
                {
                    span.CopyTo(seg.AsSpan(0, span.Length));
                }
                else
                {
                    ms.Position = 0;
                    ms.Write(span);
                }

                ms.Position = 0;
                return ms;
            }
            catch
            {
                ms.Dispose();
                throw;
            }
        }

    }


    public System.IO.MemoryStream GetSync(ReadOnlySpan<char> chars, CancellationToken cancellationToken = default)
    {
        RecyclableMemoryStreamManager mgr = GetManagerSync(cancellationToken);

        if (chars.Length == 0)
            return mgr.GetStream();

        return GetStreamFromChars(mgr, chars);

        static System.IO.MemoryStream GetStreamFromChars(RecyclableMemoryStreamManager mgr, ReadOnlySpan<char> charsSpan)
        {
            int byteCount = checked(_utf8.GetByteCount(charsSpan));
            if (byteCount == 0)
                return mgr.GetStream();

            System.IO.MemoryStream ms = mgr.GetStream(tag: null, requiredSize: byteCount);

            try
            {
                // IMPORTANT: TryGetBuffer exposes only [0..Length), not Capacity.
                ms.SetLength(byteCount);

                if (ms.TryGetBuffer(out ArraySegment<byte> seg))
                {
                    _utf8.GetBytes(charsSpan, seg.AsSpan(0, byteCount));
                    ms.Position = 0;
                    return ms;
                }

                // Fallback
                byte[] tmp = GC.AllocateUninitializedArray<byte>(byteCount);
                _utf8.GetBytes(charsSpan, tmp);
                ms.Write(tmp, 0, tmp.Length);
                ms.Position = 0;
                return ms;
            }
            catch
            {
                ms.Dispose();
                throw;
            }
        }
    }

    public ValueTask<System.IO.MemoryStream> Get(ReadOnlyMemory<char> chars, CancellationToken cancellationToken = default)
    {
        ValueTask<RecyclableMemoryStreamManager> vt = GetManager(cancellationToken);

        if (vt.IsCompletedSuccessfully)
            return new ValueTask<System.IO.MemoryStream>(GetStreamFromChars(vt.Result, chars.Span));

        return GetCharsSlow(vt, chars);

        static async ValueTask<System.IO.MemoryStream> GetCharsSlow(ValueTask<RecyclableMemoryStreamManager> mgrTask, ReadOnlyMemory<char> c)
        {
            RecyclableMemoryStreamManager mgr = await mgrTask.NoSync();
            return GetStreamFromChars(mgr, c.Span);
        }

        static System.IO.MemoryStream GetStreamFromChars(RecyclableMemoryStreamManager mgr, ReadOnlySpan<char> charsSpan)
        {
            if (charsSpan.Length == 0)
                return mgr.GetStream();

            int byteCount = _utf8.GetByteCount(charsSpan);
            if (byteCount == 0)
                return mgr.GetStream();

            System.IO.MemoryStream ms = mgr.GetStream(tag: null, requiredSize: byteCount);

            try
            {
                // IMPORTANT: TryGetBuffer exposes only [0..Length), not Capacity.
                ms.SetLength(byteCount);

                if (ms.TryGetBuffer(out ArraySegment<byte> seg))
                {
                    _utf8.GetBytes(charsSpan, seg.AsSpan(0, byteCount));
                    ms.Position = 0;
                    return ms;
                }

                // Fallback
                byte[] tmp = GC.AllocateUninitializedArray<byte>(byteCount);
                _utf8.GetBytes(charsSpan, tmp);
                ms.Write(tmp, 0, tmp.Length);
                ms.Position = 0;
                return ms;
            }
            catch
            {
                ms.Dispose();
                throw;
            }
        }
    }
}