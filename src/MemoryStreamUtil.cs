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

        // Ask for the required size up front (less internal resizing/copying)
        System.IO.MemoryStream ms = mgr.GetStream(tag: null, requiredSize: byteCount);

        // IMPORTANT: TryGetBuffer exposes only [0..Length), not Capacity.
        // Ensure Length is large enough before slicing into the segment.
        ms.SetLength(byteCount);

        if (ms.TryGetBuffer(out ArraySegment<byte> seg))
        {
            _utf8.GetBytes(str.AsSpan(), seg.AsSpan(0, byteCount));
        }
        else
        {
            // Rare for RecyclableMemoryStream, but keep correct fallback
            byte[] tmp = GC.AllocateUninitializedArray<byte>(byteCount);
            _utf8.GetBytes(str.AsSpan(), tmp);
            ms.Position = 0;
            ms.Write(tmp, 0, tmp.Length);
        }

        ms.Position = 0;
        return ms;
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
                if (memStream.TryGetBuffer(out ArraySegment<byte> seg))
                {
                    var len = checked((int)memStream.Length);
                    byte[] result = GC.AllocateUninitializedArray<byte>(len);
                    Buffer.BlockCopy(seg.Array!, seg.Offset, result, 0, len);
                    return result;
                }

                return memStream.ToArray();
            }

            // Non-MemoryStream path
            var initialSize = 0;

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
                return buffer.ToArray();
            }
            finally
            {
                await buffer.DisposeAsync()
                            .NoSync(); // always dispose the temp buffer
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

        // IMPORTANT: TryGetBuffer exposes only [0..Length), not Capacity.
        ms.SetLength(bytes.Length);

        if (ms.TryGetBuffer(out ArraySegment<byte> seg))
        {
            bytes.CopyTo(seg.AsSpan(0, bytes.Length));
        }
        else
        {
            ms.Position = 0;
            ms.Write(bytes);
        }

        ms.Position = 0;
        return ms;
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

            // IMPORTANT: TryGetBuffer exposes only [0..Length), not Capacity.
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
    }

    public System.IO.MemoryStream GetSync(ReadOnlySpan<char> chars, CancellationToken cancellationToken = default)
    {
        if (chars.Length == 0)
            return GetManagerSync(cancellationToken)
                .GetStream();

        return GetStreamFromChars(GetManagerSync(cancellationToken), chars);

        static System.IO.MemoryStream GetStreamFromChars(RecyclableMemoryStreamManager mgr, ReadOnlySpan<char> charsSpan)
        {
            int byteCount = _utf8.GetByteCount(charsSpan);
            System.IO.MemoryStream ms = mgr.GetStream(tag: null, requiredSize: byteCount);

            // IMPORTANT: TryGetBuffer exposes only [0..Length), not Capacity.
            ms.SetLength(byteCount);

            if (ms.TryGetBuffer(out ArraySegment<byte> seg))
            {
                _utf8.GetBytes(charsSpan, seg.AsSpan(0, byteCount));
            }
            else
            {
                // Fallback
                byte[] tmp = GC.AllocateUninitializedArray<byte>(byteCount);
                _utf8.GetBytes(charsSpan, tmp);
                ms.Position = 0;
                ms.Write(tmp, 0, tmp.Length);
            }

            ms.Position = 0;
            return ms;
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
            System.IO.MemoryStream ms = mgr.GetStream(tag: null, requiredSize: byteCount);

            // IMPORTANT: TryGetBuffer exposes only [0..Length), not Capacity.
            ms.SetLength(byteCount);

            if (ms.TryGetBuffer(out ArraySegment<byte> seg))
            {
                _utf8.GetBytes(charsSpan, seg.AsSpan(0, byteCount));
            }
            else
            {
                byte[] tmp = GC.AllocateUninitializedArray<byte>(byteCount);
                _utf8.GetBytes(charsSpan, tmp);
                ms.Position = 0;
                ms.Write(tmp, 0, tmp.Length);
            }

            ms.Position = 0;
            return ms;
        }
    }
}