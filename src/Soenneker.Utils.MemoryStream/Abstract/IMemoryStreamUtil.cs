using Microsoft.IO;
using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.MemoryStream.Abstract;

/// <summary>
/// Should be registered as a Singleton since this relies on a manager that does take some initialization time.
/// </summary>
/// <remarks>Be sure to dispose of the streams returned from this ASAP.</remarks>
public interface IMemoryStreamUtil : IAsyncDisposable
{
    /// <summary>
    /// Typically, not going to be used externally, but available just in case.
    /// </summary>
    /// <returns></returns>
    [Pure]
    ValueTask<RecyclableMemoryStreamManager> GetManager(CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronously returns the shared recyclable-memory-stream manager.
    /// </summary>
    /// <param name="cancellationToken">Signals that the operation should stop.</param>
    /// <returns>The shared recyclable-stream manager.</returns>
    [Pure]
    RecyclableMemoryStreamManager GetManagerSync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a fresh MemoryStream from the <see cref="RecyclableMemoryStreamManager"/>
    /// </summary>
    /// <returns></returns>
    [Pure]
    ValueTask<System.IO.MemoryStream> Get(CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronously creates an empty recyclable memory stream.
    /// </summary>
    /// <param name="cancellationToken">Signals that the operation should stop.</param>
    /// <returns>A recyclable memory stream positioned at the beginning.</returns>
    [Pure]
    System.IO.MemoryStream GetSync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a recyclable memory stream initialized from the supplied bytes and positioned for reading.
    /// </summary>
    /// <param name="bytes">Bytes used to initialize the stream.</param>
    /// <param name="cancellationToken">Signals that the operation should stop.</param>
    /// <returns>A recyclable memory stream positioned at the beginning.</returns>
    [Pure]
    ValueTask<System.IO.MemoryStream> Get(byte[] bytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronously creates a recyclable memory stream initialized from the supplied bytes and positioned for reading.
    /// </summary>
    /// <param name="bytes">Bytes used to initialize the stream.</param>
    /// <param name="cancellationToken">Signals that the operation should stop.</param>
    /// <returns>A recyclable memory stream positioned at the beginning.</returns>
    [Pure]
    System.IO.MemoryStream GetSync(byte[] bytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts to byte array (UTF8) and then converts into a MemoryStream
    /// </summary>
    /// <returns>Converts to byte array (UTF8) and then converts into a MemoryStream.</returns>
    [Pure]
    ValueTask<System.IO.MemoryStream> Get(string str, CancellationToken cancellationToken = default);

    /// <summary>
    /// Encodes the supplied string as UTF-8 into a recyclable memory stream positioned for reading.
    /// </summary>
    /// <param name="str">The JSON text.</param>
    /// <param name="cancellationToken">Signals that the operation should stop.</param>
    /// <returns>A recyclable memory stream positioned at the beginning.</returns>
    [Pure]
    System.IO.MemoryStream GetSync(string str, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies bytes from the stream's current position to the end into a new array.
    /// </summary>
    /// <remarks>A memory stream's position is preserved. Other stream types are consumed. The input is disposed unless <paramref name="keepOpen"/> is true.</remarks>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="keepOpen">True to leave the input stream open; false to dispose it after the operation.</param>
    /// <param name="cancellationToken">Signals that copying should stop.</param>
    /// <returns>The remaining stream content.</returns>
    [Pure]
    ValueTask<byte[]> GetBytesFromStream(Stream stream, bool keepOpen = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronously creates a recyclable memory stream initialized from the supplied bytes and positioned for reading.
    /// </summary>
    /// <param name="bytes">Bytes used to initialize the stream.</param>
    /// <param name="cancellationToken">Signals that the operation should stop.</param>
    /// <returns>A recyclable memory stream positioned at the beginning.</returns>
    [Pure]
    System.IO.MemoryStream GetSync(ReadOnlySpan<byte> bytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a recyclable memory stream initialized from the supplied bytes and positioned for reading.
    /// </summary>
    /// <param name="bytes">Bytes used to initialize the stream.</param>
    /// <param name="cancellationToken">Signals that the operation should stop.</param>
    /// <returns>A recyclable memory stream positioned at the beginning.</returns>
    [Pure]
    ValueTask<System.IO.MemoryStream> Get(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Encodes the supplied characters as UTF-8 into a recyclable memory stream positioned for reading.
    /// </summary>
    /// <param name="chars">Characters encoded into the stream.</param>
    /// <param name="cancellationToken">Signals that the operation should stop.</param>
    /// <returns>A recyclable memory stream positioned at the beginning.</returns>
    [Pure]
    System.IO.MemoryStream GetSync(ReadOnlySpan<char> chars, CancellationToken cancellationToken = default);

    /// <summary>
    /// Encodes the supplied characters as UTF-8 into a recyclable memory stream positioned for reading.
    /// </summary>
    /// <param name="chars">Characters encoded into the stream.</param>
    /// <param name="cancellationToken">Signals that the operation should stop.</param>
    /// <returns>A recyclable memory stream positioned at the beginning.</returns>
    [Pure]
    ValueTask<System.IO.MemoryStream> Get(ReadOnlyMemory<char> chars, CancellationToken cancellationToken = default);
}
