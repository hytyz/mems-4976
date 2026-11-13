using System.Text.Json;
using MEMS.Shared.Contracts;
using Microsoft.JSInterop;

namespace MEMS.Public.Services;

/// <summary>One selection on the visitor's ballot.</summary>
public sealed record BallotEntry(int CandidateId, int PositionId);

/// <summary>
/// The visitor's personal voting guide.
///
/// Election privacy: selections live only in the browser's <c>sessionStorage</c>. They are never
/// sent to the API and never written to SQL Server, so no one (administrators included) can see
/// them or tally them. The browser discards them when the session ends (the tab or browser closes).
///
/// Each municipality has its own guide (key <c>mems.ballot.{slug}</c>), so a guide always covers
/// one municipality, and a position never gets more picks than it has seats.
/// </summary>
public sealed class VotingGuide(IJSRuntime js)
{
    private const string KeyPrefix = "mems.ballot.";

    /// <summary>Raised after any change, with the tenant slug.</summary>
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
            // Unreadable or unavailable storage: start with an empty guide rather than fail.
            return [];
        }
    }

    /// <summary>Why the candidate cannot be added, or null when they can.</summary>
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

    /// <summary>Adds the candidate; returns an explanation instead when the seat limit is reached.</summary>
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

    /// <summary>Replaces the stored list, e.g. to drop candidates that no longer exist.</summary>
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
