using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Threading.Tasks;

namespace Blazor.Extensions.Canvas.Test.ClientSide;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var wasm = WebAssemblyHostBuilder.CreateDefault(args);
        wasm.RootComponents.Add<App>("#app");

        await wasm.Build().RunAsync();
    }
}
