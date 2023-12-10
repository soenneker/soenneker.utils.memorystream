using System.IO;
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

    public ValueTask<RecyclableMemoryStreamManager> GetManager()
    {
        return _manager.Get();
    }

    public RecyclableMemoryStreamManager GetManagerSync()
    {
        return _manager.GetSync();
    }

    public async ValueTask<System.IO.MemoryStream> Get()
    {
        System.IO.MemoryStream stream = (await GetManager().NoSync()).GetStream();
        return stream;
    }

    public System.IO.MemoryStream GetSync()
    {
        System.IO.MemoryStream stream = GetManagerSync().GetStream();
        return stream;
    }

    public async ValueTask<System.IO.MemoryStream> Get(byte[] bytes)
    {
        System.IO.MemoryStream stream = (await GetManager().NoSync()).GetStream(bytes);
        return stream;
    }

    public System.IO.MemoryStream GetSync(byte[] bytes)
    {
        System.IO.MemoryStream stream = GetManagerSync().GetStream(bytes);
        return stream;
    }

    public ValueTask<System.IO.MemoryStream> Get(string str)
    {
        byte[] bytes = str.ToBytes();
        return Get(bytes);
    }

    public System.IO.MemoryStream GetSync(string str)
    {
        byte[] bytes = str.ToBytes();
        return GetSync(bytes);
    }

    public async ValueTask<byte[]> GetBytesFromStream(Stream stream)
    {
        byte[] result;

        if (stream is System.IO.MemoryStream memStream)
        {
            result = memStream.ToArray();
            await memStream.DisposeAsync().NoSync();
            return result;
        }

        System.IO.MemoryStream memoryStream = await Get().NoSync();
        await stream.CopyToAsync(memoryStream).NoSync();
        result = memoryStream.ToArray();
        await memoryStream.DisposeAsync().NoSync();

        return result;
    }
}