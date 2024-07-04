using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IO;
using Soenneker.Extensions.String;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.AsyncSingleton;
using Soenneker.Utils.MemoryStream.Abstract;

namespace Soenneker.Utils.MemoryStream;

/// <inheritdoc cref="IMemoryStreamUtil"/>
public class MemoryStreamUtil : IMemoryStreamUtil
{
    private readonly AsyncSingleton<RecyclableMemoryStreamManager> _manager;

    public MemoryStreamUtil()
    {
        _manager = new AsyncSingleton<RecyclableMemoryStreamManager>(() => new RecyclableMemoryStreamManager());
    }

    public ValueTask<RecyclableMemoryStreamManager> GetManager(CancellationToken cancellationToken = default)
    {
        return _manager.Get(cancellationToken);
    }

    public RecyclableMemoryStreamManager GetManagerSync(CancellationToken cancellationToken = default)
    {
        return _manager.GetSync(cancellationToken);
    }

    public async ValueTask<System.IO.MemoryStream> Get(CancellationToken cancellationToken = default)
    {
        System.IO.MemoryStream stream = (await GetManager(cancellationToken).NoSync()).GetStream();
        return stream;
    }

    public System.IO.MemoryStream GetSync(CancellationToken cancellationToken = default)
    {
        System.IO.MemoryStream stream = GetManagerSync(cancellationToken).GetStream();
        return stream;
    }

    public async ValueTask<System.IO.MemoryStream> Get(byte[] bytes, CancellationToken cancellationToken = default)
    {
        System.IO.MemoryStream stream = (await GetManager(cancellationToken).NoSync()).GetStream(bytes);
        return stream;
    }

    public System.IO.MemoryStream GetSync(byte[] bytes, CancellationToken cancellationToken = default)
    {
        System.IO.MemoryStream stream = GetManagerSync(cancellationToken).GetStream(bytes);
        return stream;
    }

    public ValueTask<System.IO.MemoryStream> Get(string str, CancellationToken cancellationToken = default)
    {
        byte[] bytes = str.ToBytes();
        return Get(bytes, cancellationToken);
    }

    public System.IO.MemoryStream GetSync(string str, CancellationToken cancellationToken = default)
    {
        byte[] bytes = str.ToBytes();
        return GetSync(bytes, cancellationToken);
    }

    public async ValueTask<byte[]> GetBytesFromStream(Stream stream, CancellationToken cancellationToken = default)
    {
        byte[] result;

        if (stream is System.IO.MemoryStream memStream)
        {
            result = memStream.ToArray();
            await memStream.DisposeAsync().NoSync();
            return result;
        }

        System.IO.MemoryStream memoryStream = await Get(cancellationToken).NoSync();
        await stream.CopyToAsync(memoryStream, cancellationToken).NoSync();
        result = memoryStream.ToArray();
        await memoryStream.DisposeAsync().NoSync();

        return result;
    }
}