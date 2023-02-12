[![](https://img.shields.io/nuget/v/Soenneker.Utils.MemoryStream.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.MemoryStream/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.memorystream/main.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.utils.memorystream/actions/workflows/main.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Utils.MemoryStream.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.MemoryStream/)

# Soenneker.Utils.MemoryStream
### An easy modern MemoryStream utility

A library for management and simple access of [RecyclableMemoryStreamManager](https://github.com/microsoft/Microsoft.IO.RecyclableMemoryStream)

## Installation

```
Install-Package Soenneker.Utils.MemoryStream
```

## Usage

1. Register the interop within DI (`Program.cs`).

```csharp
public static async Task Main(string[] args)
{
    ...
    builder.Services.AddMemoryStreamUtil();
}
```

2. Inject `IMemoryStreamUtil` wherever you need `MemoryStream` services

3. Retrieve a fresh `MemoryStream` from 

Example:

```csharp
public class TestClass{

    IMemoryStreamUtil _memoryStreamUtil;

    public TestClass(IMemoryStreamUtil memoryStreamUtil)
    {
        _memoryStreamUtil = memoryStreamUtil;
    }

    public async ValueTask<MemoryStream> ReadFileIntoMemoryStream(string path)
    {
        MemoryStream memoryStream = _memoryStreamUtil.Get();

        FileStream fileStream = File.OpenRead(path);

        await fileStream.CopyToAsync(memoryStream);
    
        return memoryStream;
    }
}
```