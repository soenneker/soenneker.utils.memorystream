using System.IO;
using System.Threading.Tasks;
using Microsoft.IO;
using Nito.AsyncEx;
using Soenneker.Extensions.String;
using Soenneker.Utils.MemoryStream.Abstract;

namespace Soenneker.Utils.MemoryStream;

/// <inheritdoc cref="IMemoryStreamUtil"/>
public class MemoryStreamUtil : IMemoryStreamUtil
{
    private RecyclableMemoryStreamManager? _manager;

    private readonly AsyncLock _lock;

    public MemoryStreamUtil()
    {
        _lock = new AsyncLock();
    }

    public async ValueTask<RecyclableMemoryStreamManager> GetManager()
    {
        if (_manager != null)
            return _manager;

        using (await _lock.LockAsync())
        {
            if (_manager != null)
                return _manager;

            var manager = new RecyclableMemoryStreamManager();

            _manager = manager;
        }

        return _manager;
    }

    public RecyclableMemoryStreamManager GetManagerSync()
    {
        if (_manager != null)
            return _manager;

        using (_lock.Lock())
        {
            if (_manager != null)
                return _manager;

            var manager = new RecyclableMemoryStreamManager();

            _manager = manager;
        }

        return _manager;
    }

    public async ValueTask<System.IO.MemoryStream> Get()
    {
        System.IO.MemoryStream stream = (await GetManager()).GetStream();
        return stream;
    }

    public System.IO.MemoryStream GetSync()
    {
        System.IO.MemoryStream stream = GetManagerSync().GetStream();
        return stream;
    }

    public async ValueTask<System.IO.MemoryStream> Get(byte[] bytes)
    {
        System.IO.MemoryStream stream = (await GetManager()).GetStream(bytes);
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
        if (stream is System.IO.MemoryStream memStream)
        {
            return memStream.ToArray();
        }

        using System.IO.MemoryStream memoryStream = await Get();
        await stream.CopyToAsync(memoryStream);
        byte[] result = memoryStream.ToArray();
        return result;
    }
}