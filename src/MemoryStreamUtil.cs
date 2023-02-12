using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.IO;
using Soenneker.Extensions.String;
using Soenneker.Utils.MemoryStream.Abstract;

namespace Soenneker.Utils.MemoryStream;

/// <inheritdoc cref="IMemoryStreamUtil"/>
public class MemoryStreamUtil : IMemoryStreamUtil
{
    private readonly Lazy<RecyclableMemoryStreamManager> _manager;

    public MemoryStreamUtil()
    {
        _manager = new Lazy<RecyclableMemoryStreamManager>(() => new RecyclableMemoryStreamManager(), true);
    }

    public RecyclableMemoryStreamManager GetManager()
    {
        RecyclableMemoryStreamManager manager = _manager.Value;
        return manager;
    }

    public System.IO.MemoryStream Get()
    {
        System.IO.MemoryStream stream = GetManager().GetStream();
        return stream;
    }

    public System.IO.MemoryStream GetFromBytes(byte[] bytes)
    {
        System.IO.MemoryStream stream = GetManager().GetStream(bytes);
        return stream;
    }

    public async ValueTask<byte[]> GetBytesFromStream(Stream stream)
    {
        if (stream is System.IO.MemoryStream memStream)
            return memStream.ToArray();

        using System.IO.MemoryStream memoryStream = Get();
        await stream.CopyToAsync(memoryStream);
        byte[] result = memoryStream.ToArray();
        return result;
    }

    public System.IO.MemoryStream GetFromString(string str)
    {
        byte[] bytes = str.ToBytes();
        System.IO.MemoryStream result = GetFromBytes(bytes);
        return result;
    }
}