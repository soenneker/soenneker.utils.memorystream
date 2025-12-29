using AwesomeAssertions;
using Microsoft.IO;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Soenneker.Utils.MemoryStream.Tests;

public class MemoryStreamUtilTests
{
    private readonly MemoryStreamUtil _util;

    public MemoryStreamUtilTests()
    {
        _util = new MemoryStreamUtil();
    }

    [Fact]
    public async Task GetManager_ShouldReturnRecyclableMemoryStreamManager()
    {
        RecyclableMemoryStreamManager manager = await _util.GetManager();

        manager.Should().NotBeNull();
    }

    [Fact]
    public void GetManagerSync_ShouldReturnRecyclableMemoryStreamManager()
    {
        RecyclableMemoryStreamManager manager = _util.GetManagerSync();

        manager.Should().NotBeNull();
    }

    [Fact]
    public async Task GetManager_ShouldReturnSameInstance()
    {
        RecyclableMemoryStreamManager manager1 = await _util.GetManager();
        RecyclableMemoryStreamManager manager2 = await _util.GetManager();

        manager1.Should().BeSameAs(manager2);
    }

    [Fact]
    public void GetManagerSync_ShouldReturnSameInstance()
    {
        RecyclableMemoryStreamManager manager1 = _util.GetManagerSync();
        RecyclableMemoryStreamManager manager2 = _util.GetManagerSync();

        manager1.Should().BeSameAs(manager2);
    }

    [Fact]
    public async Task GetManager_WithCancellationToken_ShouldComplete()
    {
        using CancellationTokenSource cts = new();

        RecyclableMemoryStreamManager manager = await _util.GetManager(cts.Token);

        manager.Should().NotBeNull();
    }

    [Fact]
    public void GetManagerSync_WithCancellationToken_ShouldComplete()
    {
        using CancellationTokenSource cts = new();

        RecyclableMemoryStreamManager manager = _util.GetManagerSync(cts.Token);

        manager.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_Empty_ShouldReturnMemoryStream()
    {
        System.IO.MemoryStream stream = await _util.Get();

        stream.Should().NotBeNull();
        stream.Length.Should().Be(0);
        stream.Position.Should().Be(0);
        stream.CanRead.Should().BeTrue();
        stream.CanWrite.Should().BeTrue();
        stream.CanSeek.Should().BeTrue();
    }

    [Fact]
    public void GetSync_Empty_ShouldReturnMemoryStream()
    {
        System.IO.MemoryStream stream = _util.GetSync();

        stream.Should().NotBeNull();
        stream.Length.Should().Be(0);
        stream.Position.Should().Be(0);
        stream.CanRead.Should().BeTrue();
        stream.CanWrite.Should().BeTrue();
        stream.CanSeek.Should().BeTrue();
    }

    [Fact]
    public async Task Get_Empty_WithCancellationToken_ShouldReturnMemoryStream()
    {
        using CancellationTokenSource cts = new();

        System.IO.MemoryStream stream = await _util.Get(cts.Token);

        stream.Should().NotBeNull();
        stream.Length.Should().Be(0);
    }

    [Fact]
    public void GetSync_Empty_WithCancellationToken_ShouldReturnMemoryStream()
    {
        using CancellationTokenSource cts = new();

        System.IO.MemoryStream stream = _util.GetSync(cts.Token);

        stream.Should().NotBeNull();
        stream.Length.Should().Be(0);
    }

    [Fact]
    public async Task Get_WithByteArray_ShouldReturnMemoryStreamWithData()
    {
        byte[] data = [1, 2, 3, 4, 5];

        System.IO.MemoryStream stream = await _util.Get(data);

        stream.Should().NotBeNull();
        stream.Length.Should().Be(5);
        stream.Position.Should().Be(0);
        byte[] result = stream.ToArray();
        result.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void GetSync_WithByteArray_ShouldReturnMemoryStreamWithData()
    {
        byte[] data = [1, 2, 3, 4, 5];

        System.IO.MemoryStream stream = _util.GetSync(data);

        stream.Should().NotBeNull();
        stream.Length.Should().Be(5);
        stream.Position.Should().Be(0);
        byte[] result = stream.ToArray();
        result.Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task Get_WithEmptyByteArray_ShouldReturnEmptyMemoryStream()
    {
        byte[] data = [];

        System.IO.MemoryStream stream = await _util.Get(data);

        stream.Should().NotBeNull();
        stream.Length.Should().Be(0);
    }

    [Fact]
    public void GetSync_WithEmptyByteArray_ShouldReturnEmptyMemoryStream()
    {
        byte[] data = [];

        System.IO.MemoryStream stream = _util.GetSync(data);

        stream.Should().NotBeNull();
        stream.Length.Should().Be(0);
    }

    [Fact]
    public async Task Get_WithNullByteArray_ShouldThrowArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _util.Get((byte[])null!).AsTask());
    }

    [Fact]
    public void GetSync_WithNullByteArray_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _util.GetSync((byte[])null!));
    }

    [Fact]
    public async Task Get_WithLargeByteArray_ShouldReturnMemoryStreamWithData()
    {
        byte[] data = new byte[10000];
        new System.Random().NextBytes(data);

        System.IO.MemoryStream stream = await _util.Get(data);

        stream.Should().NotBeNull();
        stream.Length.Should().Be(10000);
        byte[] result = stream.ToArray();
        result.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void GetSync_WithLargeByteArray_ShouldReturnMemoryStreamWithData()
    {
        byte[] data = new byte[10000];
        new System.Random().NextBytes(data);

        System.IO.MemoryStream stream = _util.GetSync(data);

        stream.Should().NotBeNull();
        stream.Length.Should().Be(10000);
        byte[] result = stream.ToArray();
        result.Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task Get_WithByteArray_WithCancellationToken_ShouldReturnMemoryStream()
    {
        byte[] data = [1, 2, 3];
        using CancellationTokenSource cts = new();

        System.IO.MemoryStream stream = await _util.Get(data, cts.Token);

        stream.Should().NotBeNull();
        stream.Length.Should().Be(3);
    }

    [Fact]
    public void GetSync_WithByteArray_WithCancellationToken_ShouldReturnMemoryStream()
    {
        byte[] data = [1, 2, 3];
        using CancellationTokenSource cts = new();

        System.IO.MemoryStream stream = _util.GetSync(data, cts.Token);

        stream.Should().NotBeNull();
        stream.Length.Should().Be(3);
    }

    [Fact]
    public async Task Get_WithString_ShouldReturnMemoryStreamWithUtf8Data()
    {
        string text = "Hello, World!";

        System.IO.MemoryStream stream = await _util.Get(text);

        stream.Should().NotBeNull();
        stream.Position.Should().Be(0);
        byte[] result = stream.ToArray();
        byte[] expected = Encoding.UTF8.GetBytes(text);
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void GetSync_WithString_ShouldReturnMemoryStreamWithUtf8Data()
    {
        string text = "Hello, World!";

        System.IO.MemoryStream stream = _util.GetSync(text);

        stream.Should().NotBeNull();
        stream.Position.Should().Be(0);
        byte[] result = stream.ToArray();
        byte[] expected = Encoding.UTF8.GetBytes(text);
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Get_WithEmptyString_ShouldReturnEmptyMemoryStream()
    {
        string text = "";

        System.IO.MemoryStream stream = await _util.Get(text);

        stream.Should().NotBeNull();
        stream.Length.Should().Be(0);
    }

    [Fact]
    public void GetSync_WithEmptyString_ShouldReturnEmptyMemoryStream()
    {
        string text = "";

        System.IO.MemoryStream stream = _util.GetSync(text);

        stream.Should().NotBeNull();
        stream.Length.Should().Be(0);
    }

    [Fact]
    public async Task Get_WithNullString_ShouldThrowArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _util.Get((string)null!).AsTask());
    }

    [Fact]
    public void GetSync_WithNullString_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _util.GetSync((string)null!));
    }

    [Fact]
    public async Task Get_WithUnicodeString_ShouldReturnMemoryStreamWithUtf8Data()
    {
        string text = "Hello, 世界! 🌍";

        System.IO.MemoryStream stream = await _util.Get(text);

        stream.Should().NotBeNull();
        byte[] result = stream.ToArray();
        byte[] expected = Encoding.UTF8.GetBytes(text);
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void GetSync_WithUnicodeString_ShouldReturnMemoryStreamWithUtf8Data()
    {
        string text = "Hello, 世界! 🌍";

        System.IO.MemoryStream stream = _util.GetSync(text);

        stream.Should().NotBeNull();
        byte[] result = stream.ToArray();
        byte[] expected = Encoding.UTF8.GetBytes(text);
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Get_WithString_WithCancellationToken_ShouldReturnMemoryStream()
    {
        string text = "Test";
        using CancellationTokenSource cts = new();

        System.IO.MemoryStream stream = await _util.Get(text, cts.Token);

        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetSync_WithString_WithCancellationToken_ShouldReturnMemoryStream()
    {
        string text = "Test";
        using CancellationTokenSource cts = new();

        System.IO.MemoryStream stream = _util.GetSync(text, cts.Token);

        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Get_WithReadOnlyMemoryByte_ShouldReturnMemoryStreamWithData()
    {
        byte[] data = [10, 20, 30, 40];
        ReadOnlyMemory<byte> memory = data;

        System.IO.MemoryStream stream = await _util.Get(memory);

        stream.Should().NotBeNull();
        stream.Length.Should().Be(4);
        stream.Position.Should().Be(0);
        byte[] result = stream.ToArray();
        result.Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task Get_WithEmptyReadOnlyMemoryByte_ShouldReturnEmptyMemoryStream()
    {
        ReadOnlyMemory<byte> memory = ReadOnlyMemory<byte>.Empty;

        System.IO.MemoryStream stream = await _util.Get(memory);

        stream.Should().NotBeNull();
        stream.Length.Should().Be(0);
    }

    [Fact]
    public async Task Get_WithReadOnlyMemoryByte_WithCancellationToken_ShouldReturnMemoryStream()
    {
        byte[] data = [1, 2, 3];
        ReadOnlyMemory<byte> memory = data;
        using CancellationTokenSource cts = new();

        System.IO.MemoryStream stream = await _util.Get(memory, cts.Token);

        stream.Should().NotBeNull();
        stream.Length.Should().Be(3);
    }

    [Fact]
    public async Task Get_WithLargeReadOnlyMemoryByte_ShouldReturnMemoryStreamWithData()
    {
        byte[] data = new byte[5000];
        new System.Random().NextBytes(data);
        ReadOnlyMemory<byte> memory = data;

        System.IO.MemoryStream stream = await _util.Get(memory);

        stream.Should().NotBeNull();
        stream.Length.Should().Be(5000);
        byte[] result = stream.ToArray();
        result.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void GetSync_WithReadOnlySpanByte_ShouldReturnMemoryStreamWithData()
    {
        byte[] data = [15, 25, 35, 45];
        ReadOnlySpan<byte> span = data;

        System.IO.MemoryStream stream = _util.GetSync(span);

        stream.Should().NotBeNull();
        stream.Length.Should().Be(4);
        stream.Position.Should().Be(0);
        byte[] result = stream.ToArray();
        result.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void GetSync_WithEmptyReadOnlySpanByte_ShouldReturnEmptyMemoryStream()
    {
        ReadOnlySpan<byte> span = ReadOnlySpan<byte>.Empty;

        System.IO.MemoryStream stream = _util.GetSync(span);

        stream.Should().NotBeNull();
        stream.Length.Should().Be(0);
    }

    [Fact]
    public void GetSync_WithReadOnlySpanByte_WithCancellationToken_ShouldReturnMemoryStream()
    {
        byte[] data = [5, 10, 15];
        ReadOnlySpan<byte> span = data;
        using CancellationTokenSource cts = new();

        System.IO.MemoryStream stream = _util.GetSync(span, cts.Token);

        stream.Should().NotBeNull();
        stream.Length.Should().Be(3);
    }

    [Fact]
    public void GetSync_WithLargeReadOnlySpanByte_ShouldReturnMemoryStreamWithData()
    {
        byte[] data = new byte[3000];
        new System.Random().NextBytes(data);
        ReadOnlySpan<byte> span = data;

        System.IO.MemoryStream stream = _util.GetSync(span);

        stream.Should().NotBeNull();
        stream.Length.Should().Be(3000);
        byte[] result = stream.ToArray();
        result.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void GetSync_WithReadOnlySpanChar_ShouldReturnMemoryStreamWithUtf8Data()
    {
        string text = "Test String";
        ReadOnlySpan<char> span = text.AsSpan();

        System.IO.MemoryStream stream = _util.GetSync(span);

        stream.Should().NotBeNull();
        stream.Position.Should().Be(0);
        byte[] result = stream.ToArray();
        byte[] expected = Encoding.UTF8.GetBytes(text);
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void GetSync_WithEmptyReadOnlySpanChar_ShouldReturnEmptyMemoryStream()
    {
        ReadOnlySpan<char> span = ReadOnlySpan<char>.Empty;

        System.IO.MemoryStream stream = _util.GetSync(span);

        stream.Should().NotBeNull();
        stream.Length.Should().Be(0);
    }

    [Fact]
    public void GetSync_WithReadOnlySpanChar_WithCancellationToken_ShouldReturnMemoryStream()
    {
        string text = "Span Test";
        ReadOnlySpan<char> span = text.AsSpan();
        using CancellationTokenSource cts = new();

        System.IO.MemoryStream stream = _util.GetSync(span, cts.Token);

        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetSync_WithUnicodeReadOnlySpanChar_ShouldReturnMemoryStreamWithUtf8Data()
    {
        string text = "Test 测试 🎉";
        ReadOnlySpan<char> span = text.AsSpan();

        System.IO.MemoryStream stream = _util.GetSync(span);

        stream.Should().NotBeNull();
        byte[] result = stream.ToArray();
        byte[] expected = Encoding.UTF8.GetBytes(text);
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Get_WithReadOnlyMemoryChar_ShouldReturnMemoryStreamWithUtf8Data()
    {
        string text = "Memory Test";
        ReadOnlyMemory<char> memory = text.AsMemory();

        System.IO.MemoryStream stream = await _util.Get(memory);

        stream.Should().NotBeNull();
        stream.Position.Should().Be(0);
        byte[] result = stream.ToArray();
        byte[] expected = Encoding.UTF8.GetBytes(text);
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Get_WithEmptyReadOnlyMemoryChar_ShouldReturnEmptyMemoryStream()
    {
        ReadOnlyMemory<char> memory = ReadOnlyMemory<char>.Empty;

        System.IO.MemoryStream stream = await _util.Get(memory);

        stream.Should().NotBeNull();
        stream.Length.Should().Be(0);
    }

    [Fact]
    public async Task Get_WithReadOnlyMemoryChar_WithCancellationToken_ShouldReturnMemoryStream()
    {
        string text = "Test";
        ReadOnlyMemory<char> memory = text.AsMemory();
        using CancellationTokenSource cts = new();

        System.IO.MemoryStream stream = await _util.Get(memory, cts.Token);

        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Get_WithUnicodeReadOnlyMemoryChar_ShouldReturnMemoryStreamWithUtf8Data()
    {
        string text = "Memory 测试 🚀";
        ReadOnlyMemory<char> memory = text.AsMemory();

        System.IO.MemoryStream stream = await _util.Get(memory);

        stream.Should().NotBeNull();
        byte[] result = stream.ToArray();
        byte[] expected = Encoding.UTF8.GetBytes(text);
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetBytesFromStream_WithMemoryStream_ShouldReturnByteArray()
    {
        byte[] originalData = [1, 2, 3, 4, 5];
        using System.IO.MemoryStream inputStream = new(originalData);

        byte[] result = await _util.GetBytesFromStream(inputStream);

        result.Should().BeEquivalentTo(originalData);
        inputStream.CanRead.Should().BeFalse(); // Stream should be disposed
    }

    [Fact]
    public async Task GetBytesFromStream_WithMemoryStreamAtPosition_ShouldReturnRemainingBytes()
    {
        byte[] originalData = [1, 2, 3, 4, 5];
        using System.IO.MemoryStream inputStream = new(originalData);
        inputStream.Position = 2; // Skip first 2 bytes

        byte[] result = await _util.GetBytesFromStream(inputStream);

        result.Should().BeEquivalentTo([3, 4, 5]);
        inputStream.CanRead.Should().BeFalse(); // Stream should be disposed
    }

    [Fact]
    public async Task GetBytesFromStream_WithMemoryStreamAtEnd_ShouldReturnEmptyArray()
    {
        byte[] originalData = [1, 2, 3];
        using System.IO.MemoryStream inputStream = new(originalData);
        inputStream.Position = inputStream.Length; // At end

        byte[] result = await _util.GetBytesFromStream(inputStream);

        result.Should().BeEmpty();
        inputStream.CanRead.Should().BeFalse(); // Stream should be disposed
    }

    [Fact]
    public async Task GetBytesFromStream_WithEmptyMemoryStream_ShouldReturnEmptyArray()
    {
        using System.IO.MemoryStream inputStream = new();

        byte[] result = await _util.GetBytesFromStream(inputStream);

        result.Should().BeEmpty();
        inputStream.CanRead.Should().BeFalse(); // Stream should be disposed
    }

    [Fact]
    public async Task GetBytesFromStream_WithKeepOpenTrue_ShouldNotDisposeStream()
    {
        byte[] originalData = [1, 2, 3];
        using System.IO.MemoryStream inputStream = new(originalData);

        byte[] result = await _util.GetBytesFromStream(inputStream, keepOpen: true);

        result.Should().BeEquivalentTo(originalData);
        inputStream.CanRead.Should().BeTrue(); // Stream should still be open
    }

    [Fact]
    public async Task GetBytesFromStream_WithKeepOpenFalse_ShouldDisposeStream()
    {
        byte[] originalData = [1, 2, 3];
        using System.IO.MemoryStream inputStream = new(originalData);

        byte[] result = await _util.GetBytesFromStream(inputStream, keepOpen: false);

        result.Should().BeEquivalentTo(originalData);
        inputStream.CanRead.Should().BeFalse(); // Stream should be disposed
    }

    [Fact]
    public async Task GetBytesFromStream_WithNullStream_ShouldThrowArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _util.GetBytesFromStream(null!).AsTask());
    }

    [Fact]
    public async Task GetBytesFromStream_WithFileStream_ShouldReturnByteArray()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            byte[] originalData = [10, 20, 30, 40];
            await File.WriteAllBytesAsync(tempFile, originalData);

            using FileStream fileStream = new(tempFile, FileMode.Open, FileAccess.Read);

            byte[] result = await _util.GetBytesFromStream(fileStream, keepOpen: true);

            result.Should().BeEquivalentTo(originalData);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetBytesFromStream_WithLargeMemoryStream_ShouldReturnByteArray()
    {
        byte[] originalData = new byte[10000];
        new System.Random().NextBytes(originalData);
        using System.IO.MemoryStream inputStream = new(originalData);

        byte[] result = await _util.GetBytesFromStream(inputStream);

        result.Should().BeEquivalentTo(originalData);
        result.Length.Should().Be(10000);
    }

    [Fact]
    public async Task GetBytesFromStream_WithCancellationToken_ShouldReturnByteArray()
    {
        byte[] originalData = [1, 2, 3, 4];
        using System.IO.MemoryStream inputStream = new(originalData);
        using CancellationTokenSource cts = new();

        byte[] result = await _util.GetBytesFromStream(inputStream, cancellationToken: cts.Token);

        result.Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public async Task GetBytesFromStream_WithNonSeekableStream_ShouldReturnByteArray()
    {
        byte[] originalData = [5, 10, 15, 20];
        using System.IO.MemoryStream baseStream = new(originalData);
        using NonSeekableStream nonSeekableStream = new(baseStream);

        byte[] result = await _util.GetBytesFromStream(nonSeekableStream, keepOpen: true);

        result.Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public async Task GetBytesFromStream_WithMemoryStreamInvalidPosition_ShouldThrowInvalidOperationException()
    {
        byte[] originalData = [1, 2, 3];
        using InvalidPositionStream invalidStream = new(originalData, 10); // Position 10, length 3

        await Assert.ThrowsAsync<InvalidOperationException>(() => _util.GetBytesFromStream(invalidStream).AsTask());
    }

}