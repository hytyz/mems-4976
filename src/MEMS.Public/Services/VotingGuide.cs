using System.Text.Json;
using MEMS.Shared.Contracts;
using Microsoft.JSInterop;

namespace MEMS.Public.Services;

public sealed record BallotEntry(int CandidateId, int PositionId);

public sealed class VotingGuide(IJSRuntime js)
{
    private const string KeyPrefix = "mems.ballot.";

    public event Action<string>? Changed;

    public async Task<List<BallotEntry>> GetAsync(string slug)
    {
        try
        {
            string? json = await js.InvokeAsync<string?>("sessionStorage.getItem", Key(slug));
            return string.IsNullOrEmpty(json) ? [] : JsonSerializer.Deserialize<List<BallotEntry>>(json) ?? [];
        }
        catch (Exception ex) when (ex is JsonException or JSException)
        {
            return [];
        }
    }

    public static string? GetBlockReason(IReadOnlyCollection<BallotEntry> selections, CandidateDto candidate)
    {
        if (selections.Any(s => s.CandidateId == candidate.Id))
        {
            return null;
        }

        int chosen = selections.Count(s => s.PositionId == candidate.PositionId);
        if (chosen < candidate.SeatsAvailable)
        {
            return null;
        }

        return candidate.SeatsAvailable == 1
            ? $"You have already chosen a candidate for {candidate.PositionName}. Only one can be elected, so remove your current choice first."
            : $"You have already chosen {chosen} candidates for {candidate.PositionName}, one for each of the {candidate.SeatsAvailable} seats. Remove one first.";
    }

    public async Task<string?> AddAsync(string slug, CandidateDto candidate)
    {
        var selections = await GetAsync(slug);
        if (GetBlockReason(selections, candidate) is { } reason)
        {
            return reason;
        }

        if (selections.All(s => s.CandidateId != candidate.Id))
        {
            selections.Add(new BallotEntry(candidate.Id, candidate.PositionId));
            await SaveAsync(slug, selections);
        }

        return null;
    }

    public async Task RemoveAsync(string slug, int candidateId)
    {
        var selections = await GetAsync(slug);
        if (selections.RemoveAll(s => s.CandidateId == candidateId) > 0)
        {
            await SaveAsync(slug, selections);
        }
    }

    public Task ReplaceAsync(string slug, List<BallotEntry> selections) => SaveAsync(slug, selections);

    public async Task ClearAsync(string slug)
    {
        await js.InvokeVoidAsync("sessionStorage.removeItem", Key(slug));
        Changed?.Invoke(slug);
    }

    private async Task SaveAsync(string slug, List<BallotEntry> selections)
    {
        await js.InvokeVoidAsync("sessionStorage.setItem", Key(slug), JsonSerializer.Serialize(selections));
        Changed?.Invoke(slug);
    }

    private static string Key(string slug) => KeyPrefix + slug.ToLowerInvariant();
}
