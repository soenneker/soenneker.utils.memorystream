using System.Diagnostics.Contracts;
using System.IO;
using System.Threading.Tasks;
using Microsoft.IO;

namespace Soenneker.Utils.MemoryStream.Abstract;

/// <summary>
/// Should be registered as a Singleton since this relies on a manager that does take some initialization time.
/// </summary>
public interface IMemoryStreamUtil
{
    /// <summary>
    /// Typically not going to be used externally, but available just in case.
    /// </summary>
    /// <returns></returns>
    [Pure]
    RecyclableMemoryStreamManager GetManager();

    /// <summary>
    /// Retrieves a fresh MemoryStream from the <see cref="RecyclableMemoryStreamManager"/>
    /// </summary>
    /// <returns></returns>
    [Pure]
    System.IO.MemoryStream Get();

    [Pure]
    System.IO.MemoryStream Get(byte[] bytes);

    /// <summary>
    /// If it's a MemoryStream, simply calls ToArray()... if it's not it copies the stream into a MemoryStream and then converts into a byte array.
    /// </summary>
    [Pure]
    ValueTask<byte[]> GetBytesFromStream(Stream stream);

    /// <summary>
    /// Converts to byte array (UTF8) and then converts into a MemoryStream
    /// </summary>
    [Pure]
    System.IO.MemoryStream Get(string str);
}