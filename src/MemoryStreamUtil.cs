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
public sealed class MemoryStreamUtil : IMemoryStreamUtil
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
        return (await GetManager(cancellationToken).NoSync()).GetStream();
    }

    public System.IO.MemoryStream GetSync(CancellationToken cancellationToken = default)
    {
        return GetManagerSync(cancellationToken).GetStream();
    }

    public async ValueTask<System.IO.MemoryStream> Get(byte[] bytes, CancellationToken cancellationToken = default)
    {
        return (await GetManager(cancellationToken).NoSync()).GetStream(bytes);
    }

    public System.IO.MemoryStream GetSync(byte[] bytes, CancellationToken cancellationToken = default)
    {
        return GetManagerSync(cancellationToken).GetStream(bytes);
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

    public async ValueTask<byte[]> GetBytesFromStream(Stream stream, bool keepOpen = false, CancellationToken cancellationToken = default)
    {
        byte[] result;

        if (stream is System.IO.MemoryStream memStream)
        {
            result = memStream.ToArray();

            if (!keepOpen)
                await memStream.DisposeAsync().NoSync();
            
            return result;
        }

        System.IO.MemoryStream memoryStream = await Get(cancellationToken).NoSync();
        await stream.CopyToAsync(memoryStream, cancellationToken).NoSync();
        result = memoryStream.ToArray();

        if (!keepOpen)
            await memoryStream.DisposeAsync().NoSync();

        return result;
    }
}