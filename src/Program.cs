using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Scop;
using Scop.Services;
using Tavenem.Blazor.IndexedDB;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddTavenemFramework();

builder.Services.AddIndexedDbService();
builder.Services.AddKeyedScoped(
    "scop_v1",
    (provider, name) => new IndexedDb(
        "scop",
        provider.GetRequiredService<IndexedDbService>(),
        [DataService.ObjectStoreName],
        1,
        ScopSerializerOptions.Instance));
builder.Services.AddIndexedDb(
    "scop",
    [DataService.ObjectStoreName],
    2,
    ScopSerializerOptions.Instance);

builder.Services.AddScoped<ScopJsInterop>();
builder.Services.AddScoped<DataService>();
builder.Services.AddScoped<DataMigration>();

var host = builder.Build();

var migration = host.Services.GetRequiredService<DataMigration>();
await migration.UpgradeAsync();

await builder.Build().RunAsync();
