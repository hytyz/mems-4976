using MEMS.Public;
using MEMS.Public.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddServiceDiscovery();
builder.Services.AddHttpClient<ApiClient>(client => client.BaseAddress = new Uri("https+http://api"))
    .AddServiceDiscovery();

string apiBase = builder.Configuration["Services:api:https:0"]
    ?? builder.Configuration["Services:api:http:0"]
    ?? throw new InvalidOperationException("The API address (Services:api) is missing. Start the solution through MEMS.AppHost.");
builder.Services.AddSingleton(new ApiUrls(new Uri(apiBase.TrimEnd('/') + "/")));

builder.Services.AddScoped<TenantCache>();
builder.Services.AddScoped<VotingGuide>();

await builder.Build().RunAsync();
