using System.Net;
using System.Net.Http.Json;
using MEMS.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace MEMS.Public.Services;

public sealed class ApiUnavailableException()
    : Exception("Election information could not be loaded right now. Please check your connection and try again.");

public sealed class ApiClient(HttpClient http, ILogger<ApiClient> logger)
{
    public Task<List<MunicipalityDto>?> GetMunicipalitiesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<List<MunicipalityDto>>("api/municipalities", cancellationToken);

    public Task<MunicipalityDto?> GetTenantAsync(string slug, CancellationToken cancellationToken = default) =>
        GetAsync<MunicipalityDto>(TenantPath(slug), cancellationToken);

    public async Task<List<PositionDto>> GetPositionsAsync(string slug, CancellationToken cancellationToken = default) =>
        await GetAsync<List<PositionDto>>($"{TenantPath(slug)}/positions", cancellationToken) ?? [];

    public async Task<PagedResult<CandidateDto>> SearchCandidatesAsync(
        string slug, CandidateQuery query, CancellationToken cancellationToken = default)
    {
        var parameters = new List<string> { $"page={query.Page}", $"pageSize={query.PageSize}" };
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            parameters.Add($"search={Uri.EscapeDataString(query.Search.Trim())}");
        }

        if (query.PositionId is int positionId)
        {
            parameters.Add($"positionId={positionId}");
        }

        return await GetAsync<PagedResult<CandidateDto>>($"{TenantPath(slug)}/candidates?{string.Join('&', parameters)}", cancellationToken)
               ?? new PagedResult<CandidateDto>();
    }

    public async Task<List<CandidateDto>> GetAllCandidatesAsync(string slug, CancellationToken cancellationToken = default)
    {
        var all = new List<CandidateDto>();
        int page = 1;
        PagedResult<CandidateDto> result;
        do
        {
            result = await SearchCandidatesAsync(slug, new CandidateQuery { Page = page, PageSize = CandidateQuery.MaxPageSize }, cancellationToken);
            all.AddRange(result.Items);
            page++;
        }
        while (page <= result.TotalPages);

        return all;
    }

    public Task<CandidateDto?> GetCandidateAsync(string slug, int id, CancellationToken cancellationToken = default) =>
        GetAsync<CandidateDto>($"{TenantPath(slug)}/candidates/{id}", cancellationToken);

    private static string TenantPath(string slug) => $"api/{Uri.EscapeDataString(slug.Trim().ToLowerInvariant())}";

    private async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.GetAsync(path, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "The API could not be reached for {Path}", path);
            throw new ApiUnavailableException();
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return default;
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("The API answered {StatusCode} for {Path}", (int)response.StatusCode, path);
                throw new ApiUnavailableException();
            }

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        }
    }
}

public sealed class ApiUrls(Uri apiBase)
{
    public string? Image(string slug, Guid? imageId) =>
        imageId is null ? null : new Uri(apiBase, $"api/{Uri.EscapeDataString(slug)}/images/{imageId}").ToString();
}

public sealed class TenantCache(ApiClient api)
{
    private readonly Dictionary<string, Task<MunicipalityDto?>> _tenants = new(StringComparer.OrdinalIgnoreCase);

    public Task<MunicipalityDto?> GetAsync(string slug)
    {
        if (!_tenants.TryGetValue(slug, out var task) || task.IsFaulted)
        {
            task = api.GetTenantAsync(slug);
            _tenants[slug] = task;
        }

        return task;
    }
}
