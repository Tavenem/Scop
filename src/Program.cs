using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Scop;
using Tavenem.Blazor.IndexedDB;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddTavenemFramework();

builder.Services.AddIndexedDb(
    new IndexedDb("scop", 1),
    ScopSerializerOptions.Instance);

builder.Services.AddScoped<ScopJsInterop>();
builder.Services.AddScoped<DataService>();

await builder.Build().RunAsync();
