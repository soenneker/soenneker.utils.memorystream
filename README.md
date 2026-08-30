[![](https://img.shields.io/nuget/v/Soenneker.Utils.MemoryStream.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.MemoryStream/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.memorystream/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.utils.memorystream/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Utils.MemoryStream.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.MemoryStream/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.memorystream/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.utils.memorystream/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Utils.MemoryStream
### An easy modern MemoryStream utility

A library for management and simple access of [RecyclableMemoryStreamManager](https://github.com/microsoft/Microsoft.IO.RecyclableMemoryStream)

## Installation

```
dotnet add package Soenneker.Utils.MemoryStream
```

## Registration

```csharp
using Soenneker.Utils.MemoryStream.Registrars;

services.AddMemoryStreamUtilAsSingleton();
```

The manager is designed to be shared, so singleton registration is the normal choice. A scoped
registrar is also available when the utility itself must follow a scope.

## Rent and return a stream

Inject `IMemoryStreamUtil`, then dispose every returned stream promptly so its buffers return to
the manager:

```csharp
using Soenneker.Utils.MemoryStream.Abstract;
using System.Text;

public sealed class PayloadReader
{
    private readonly IMemoryStreamUtil _memoryStreams;

    public PayloadReader(IMemoryStreamUtil memoryStreams)
    {
        _memoryStreams = memoryStreams;
    }

    public async ValueTask<string> ReadFile(string path, CancellationToken cancellationToken)
    {
        await using FileStream source = File.OpenRead(path);
        await using MemoryStream buffer = await _memoryStreams.Get(cancellationToken);

        await source.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        using var reader = new StreamReader(buffer, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
```

`Get()`/`GetSync()` return a fresh empty recyclable `MemoryStream`. Overloads accepting bytes or
text populate the stream and leave its position at zero; strings and character memory are encoded
as UTF-8. The asynchronous overloads are useful while the shared manager is being initialized,
but the returned stream is still a normal synchronous `MemoryStream`.

## Copy a stream to bytes

```csharp
byte[] remaining = await memoryStreams.GetBytesFromStream(
    source,
    keepOpen: true,
    cancellationToken);
```

Conversion begins at the input stream's current position. The default `keepOpen: false` transfers
ownership to the utility and disposes the input even when copying fails; pass `true` when the
caller must continue using it. A `MemoryStream` is copied without changing its position. Other
stream types are consumed through `CopyToAsync`, so their position advances.

The byte-array result is a separate allocation and remains valid after any source or recyclable
stream is disposed. `GetManager` is exposed for advanced Microsoft.IO configuration or APIs, but
ordinary callers should rent streams through the utility.
