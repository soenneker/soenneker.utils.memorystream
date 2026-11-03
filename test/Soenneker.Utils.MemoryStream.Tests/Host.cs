using System.Threading.Tasks;
using Soenneker.TestHosts.Unit;
using Soenneker.Utils.MemoryStream.Registrars;

namespace Soenneker.Utils.MemoryStream.Tests;

public sealed class Host : UnitTestHost
{
    public override Task InitializeAsync()
    {
        Services.AddMemoryStreamUtilAsSingleton();
        return base.InitializeAsync();
    }
}
