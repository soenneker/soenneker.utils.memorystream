using System.Diagnostics.Contracts;
using System.IO;
using System.Threading.Tasks;
using Microsoft.IO;

namespace Soenneker.Utils.MemoryStream.Abstract;

/// <summary>
/// Should be registered as a Singleton since this relies on a manager that does take some initialization time.
/// </summary>
/// <remarks>Be sure to dispose of the streams returned from this ASAP.</remarks>
public interface IMemoryStreamUtil
{
    /// <summary>
    /// Typically not going to be used externally, but available just in case.
    /// </summary>
    /// <returns></returns>
    [Pure]
    ValueTask<RecyclableMemoryStreamManager> GetManager();

    /// <inheritdoc cref="GetManager"/>
    [Pure]
    RecyclableMemoryStreamManager GetManagerSync();

    /// <summary>
    /// Retrieves a fresh MemoryStream from the <see cref="RecyclableMemoryStreamManager"/>
    /// </summary>
    /// <returns></returns>
    [Pure]
    ValueTask<System.IO.MemoryStream> Get();

    /// <summary>
    /// Use async version <see cref="Get()"/> if possible. <para/>
    /// <inheritdoc cref="Get()"/>
    /// </summary>
    [Pure]
    System.IO.MemoryStream GetSync();

    [Pure]
    ValueTask<System.IO.MemoryStream> Get(byte[] bytes);

    /// <summary>
    /// Use async version <see cref="Get()"/> if possible. <para/>
    /// <inheritdoc cref="Get()"/>
    /// </summary>
    [Pure]
    System.IO.MemoryStream GetSync(byte[] bytes);

    /// <summary>
    /// Converts to byte array (UTF8) and then converts into a MemoryStream
    /// </summary>
    [Pure]
    ValueTask<System.IO.MemoryStream> Get(string str);

    /// <summary>
    /// Use async version <see cref="Get()"/> if possible. <para/>
    /// <inheritdoc cref="Get()"/>
    /// </summary>
    [Pure]
    System.IO.MemoryStream GetSync(string str);

    /// <summary>
    /// If it's a MemoryStream, simply calls ToArray()... if it's not it copies the stream into a MemoryStream and then converts into a byte array.
    /// </summary>
    /// <remarks>This will dispose of the incoming stream since it's assumed that usage of it is complete after moving to a byte array.</remarks>
    [Pure]
    ValueTask<byte[]> GetBytesFromStream(Stream stream);
}