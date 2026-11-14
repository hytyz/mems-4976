using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MEMS.AppHost;

public static class BrowserServiceReference
{
    public static IResourceBuilder<ProjectResource> WithBrowserServiceReference(
        this IResourceBuilder<ProjectResource> client, IResourceBuilder<ProjectResource> service)
    {
        string clientDirectory = Path.GetDirectoryName(client.Resource.GetProjectMetadata().ProjectPath)!;
        string settingsPath = Path.Combine(clientDirectory, "wwwroot", "appsettings.json");

        client.OnBeforeResourceStarted(async (_, started, cancellationToken) =>
        {
            var logger = started.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(BrowserServiceReference));

            var endpoints = new JsonObject();
            foreach (string scheme in new[] { "https", "http" })
            {
                var endpoint = service.GetEndpoint(scheme);
                if (endpoint.Exists)
                {
                    string? url = await endpoint.GetValueAsync(cancellationToken);
                    if (url is not null)
                    {
                        endpoints[scheme] = new JsonArray(url);
                    }
                }
            }

            JsonObject root = File.Exists(settingsPath)
                ? JsonNode.Parse(await File.ReadAllTextAsync(settingsPath, cancellationToken)) as JsonObject ?? []
                : [];
            string before = root.ToJsonString();

            var services = root["Services"] as JsonObject ?? [];
            services[service.Resource.Name] = endpoints;
            root["Services"] = services;

            if (root.ToJsonString() != before)
            {
                await File.WriteAllTextAsync(
                    settingsPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine, cancellationToken);
                logger.LogInformation("Wrote service discovery for {Service} to {Path}", service.Resource.Name, settingsPath);
            }
        });

        return client;
    }
}
