using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Scop;
using Tavenem.Blazor.IndexedDB;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddIndexedDb(new IndexedDb<string>("scop", 1));

builder.Services.AddMudServices();
builder.Services.AddMarkdownEditor();
builder.Services.AddScoped<ScopJsInterop>();
builder.Services.AddScoped<DataService>();

await builder.Build().RunAsync().ConfigureAwait(false);
