using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Microsoft.IO;
using Soenneker.Utils.AsyncSingleton;
using Soenneker.Utils.MemoryStream.Abstract;
using System;
using System.IO;
using System.Runtime.CompilerServices;
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
        // No closure: method group to static method.
        _manager = new AsyncSingleton<RecyclableMemoryStreamManager>(CreateManager);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static RecyclableMemoryStreamManager CreateManager() => new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<RecyclableMemoryStreamManager> GetManager(CancellationToken cancellationToken = default) =>
        _manager.Get(cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RecyclableMemoryStreamManager GetManagerSync(CancellationToken cancellationToken = default) =>
        _manager.GetSync(cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<System.IO.MemoryStream> Get(CancellationToken cancellationToken = default)
    {
        ValueTask<RecyclableMemoryStreamManager> vt = GetManager(cancellationToken);

        if (vt.IsCompletedSuccessfully)
            return new ValueTask<System.IO.MemoryStream>(vt.Result.GetStream());

        return GetSlow(vt);
    }

    private static async ValueTask<System.IO.MemoryStream> GetSlow(ValueTask<RecyclableMemoryStreamManager> mgrTask)
    {
        RecyclableMemoryStreamManager mgr = await mgrTask.NoSync();
        return mgr.GetStream();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public System.IO.MemoryStream GetSync(CancellationToken cancellationToken = default) =>
        GetManagerSync(cancellationToken)
            .GetStream();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<System.IO.MemoryStream> Get(byte[] bytes, CancellationToken cancellationToken = default)
    {
        if (bytes is null)
            throw new ArgumentNullException(nameof(bytes));

        ValueTask<RecyclableMemoryStreamManager> vt = GetManager(cancellationToken);

        if (vt.IsCompletedSuccessfully)
            return new ValueTask<System.IO.MemoryStream>(vt.Result.GetStream(bytes));

        return GetBytesSlow(vt, bytes);
    }

    private static async ValueTask<System.IO.MemoryStream> GetBytesSlow(ValueTask<RecyclableMemoryStreamManager> mgrTask, byte[] b)
    {
        RecyclableMemoryStreamManager mgr = await mgrTask.NoSync();
        return mgr.GetStream(b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public System.IO.MemoryStream GetSync(byte[] bytes, CancellationToken cancellationToken = default)
    {
        if (bytes is null)
            throw new ArgumentNullException(nameof(bytes));

        return GetManagerSync(cancellationToken)
            .GetStream(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<System.IO.MemoryStream> Get(string str, CancellationToken cancellationToken = default)
    {
        if (str is null)
            throw new ArgumentNullException(nameof(str));

        ValueTask<RecyclableMemoryStreamManager> vt = GetManager(cancellationToken);

        if (vt.IsCompletedSuccessfully)
            return new ValueTask<System.IO.MemoryStream>(GetStreamFromString(vt.Result, str));

        return GetStringSlow(vt, str);
    }

    private static async ValueTask<System.IO.MemoryStream> GetStringSlow(ValueTask<RecyclableMemoryStreamManager> mgrTask, string s)
    {
        RecyclableMemoryStreamManager mgr = await mgrTask.NoSync();
        return GetStreamFromString(mgr, s);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public System.IO.MemoryStream GetSync(string str, CancellationToken cancellationToken = default)
    {
        if (str is null)
            throw new ArgumentNullException(nameof(str));

        return GetStreamFromString(GetManagerSync(cancellationToken), str);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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
            ms.Position = 0;
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
            if (stream is System.IO.MemoryStream memStream)
                return GetBytesFromMemoryStream(memStream);

            return await GetBytesFromNonMemoryStream(stream, cancellationToken)
                .NoSync();
        }
        finally
        {
            if (!keepOpen)
                await stream.DisposeAsync()
                            .NoSync();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static byte[] GetBytesFromMemoryStream(System.IO.MemoryStream memStream)
    {
        long pos64 = memStream.CanSeek ? memStream.Position : 0;
        long len64 = memStream.Length;

        if ((ulong)pos64 > (ulong)len64)
            throw new InvalidOperationException("Stream position is out of bounds.");

        int remaining = checked((int)(len64 - pos64));

        if (remaining == 0)
            return Array.Empty<byte>();

        if (memStream.TryGetBuffer(out ArraySegment<byte> seg))
        {
            byte[] result = GC.AllocateUninitializedArray<byte>(remaining);
            int srcOffset = checked(seg.Offset + (int)pos64);
            Buffer.BlockCopy(seg.Array!, srcOffset, result, 0, remaining);
            return result;
        }

        // Fallback (position-aware) without copying the entire stream (MemoryStream.ToArray()).
        // Preserve caller's Position semantics.
        long originalPos = memStream.CanSeek ? memStream.Position : 0;
        byte[] resultFallback = GC.AllocateUninitializedArray<byte>(remaining);

        try
        {
            if (memStream.CanSeek)
                memStream.Position = pos64;

            var totalRead = 0;

            while (totalRead < remaining)
            {
                int read = memStream.Read(resultFallback, totalRead, remaining - totalRead);
                if (read == 0)
                    break;
                totalRead += read;
            }

            if (totalRead != remaining)
            {
                // Extremely defensive: if stream length changed mid-read (shouldn't happen for MemoryStream),
                // return the bytes we did read.
                byte[] truncated = new byte[totalRead];
                Buffer.BlockCopy(resultFallback, 0, truncated, 0, totalRead);
                return truncated;
            }

            return resultFallback;
        }
        finally
        {
            if (memStream.CanSeek)
                memStream.Position = originalPos;
        }
    }

    private async ValueTask<byte[]> GetBytesFromNonMemoryStream(Stream stream, CancellationToken cancellationToken)
    {
        int initialSize = GetRemainingSizeHint(stream);

        RecyclableMemoryStreamManager mgr = await GetManager(cancellationToken)
            .NoSync();

        System.IO.MemoryStream buffer = initialSize > 0 ? mgr.GetStream(tag: null, requiredSize: initialSize) : mgr.GetStream();

        try
        {
            await stream.CopyToAsync(buffer, cancellationToken)
                        .NoSync();

            int length = checked((int)buffer.Length);
            if (length == 0)
                return Array.Empty<byte>();

            if (buffer.TryGetBuffer(out ArraySegment<byte> seg))
            {
                byte[] result = GC.AllocateUninitializedArray<byte>(length);
                Buffer.BlockCopy(seg.Array!, seg.Offset, result, 0, length);
                return result;
            }

            return buffer.ToArray();
        }
        finally
        {
            await buffer.DisposeAsync()
                        .NoSync();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetRemainingSizeHint(Stream stream)
    {
        if (!stream.CanSeek)
            return 0;

        long remaining = stream.Length - stream.Position;
        if (remaining <= 0 || (ulong)remaining > int.MaxValue)
            return 0;

        return (int)remaining;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public System.IO.MemoryStream GetSync(ReadOnlySpan<byte> bytes, CancellationToken cancellationToken = default)
    {
        RecyclableMemoryStreamManager mgr = GetManagerSync(cancellationToken);

        if (bytes.Length == 0)
            return mgr.GetStream();

        return GetStreamFromBytes(mgr, bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static System.IO.MemoryStream GetStreamFromBytes(RecyclableMemoryStreamManager mgr, ReadOnlySpan<byte> span)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<System.IO.MemoryStream> Get(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
    {
        ValueTask<RecyclableMemoryStreamManager> vt = GetManager(cancellationToken);

        if (vt.IsCompletedSuccessfully)
            return new ValueTask<System.IO.MemoryStream>(GetStreamFromBytes(vt.Result, bytes.Span));

        return GetBytesMemorySlow(vt, bytes);
    }

    private static async ValueTask<System.IO.MemoryStream> GetBytesMemorySlow(ValueTask<RecyclableMemoryStreamManager> mgrTask, ReadOnlyMemory<byte> b)
    {
        RecyclableMemoryStreamManager mgr = await mgrTask.NoSync();
        return GetStreamFromBytes(mgr, b.Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public System.IO.MemoryStream GetSync(ReadOnlySpan<char> chars, CancellationToken cancellationToken = default)
    {
        RecyclableMemoryStreamManager mgr = GetManagerSync(cancellationToken);

        if (chars.Length == 0)
            return mgr.GetStream();

        return GetStreamFromChars(mgr, chars);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static System.IO.MemoryStream GetStreamFromChars(RecyclableMemoryStreamManager mgr, ReadOnlySpan<char> charsSpan)
    {
        if (charsSpan.Length == 0)
            return mgr.GetStream();

        int byteCount = checked(_utf8.GetByteCount(charsSpan));
        if (byteCount == 0)
            return mgr.GetStream();

        System.IO.MemoryStream ms = mgr.GetStream(tag: null, requiredSize: byteCount);

        try
        {
            ms.SetLength(byteCount);

            if (ms.TryGetBuffer(out ArraySegment<byte> seg))
            {
                _utf8.GetBytes(charsSpan, seg.AsSpan(0, byteCount));
                ms.Position = 0;
                return ms;
            }

            byte[] tmp = GC.AllocateUninitializedArray<byte>(byteCount);
            _utf8.GetBytes(charsSpan, tmp);
            ms.Position = 0;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<System.IO.MemoryStream> Get(ReadOnlyMemory<char> chars, CancellationToken cancellationToken = default)
    {
        ValueTask<RecyclableMemoryStreamManager> vt = GetManager(cancellationToken);

        if (vt.IsCompletedSuccessfully)
            return new ValueTask<System.IO.MemoryStream>(GetStreamFromChars(vt.Result, chars.Span));

        return GetCharsMemorySlow(vt, chars);
    }

    private static async ValueTask<System.IO.MemoryStream> GetCharsMemorySlow(ValueTask<RecyclableMemoryStreamManager> mgrTask, ReadOnlyMemory<char> c)
    {
        RecyclableMemoryStreamManager mgr = await mgrTask.NoSync();
        return GetStreamFromChars(mgr, c.Span);
    }
}