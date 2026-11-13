using MEMS.Public.Services;
using MEMS.Shared.Contracts;

namespace MEMS.Public.Components;

/// <summary>One position on the visitor's guide with the candidates they picked for it.</summary>
public sealed record GuideGroup(PositionDto Position, List<CandidateDto> Candidates);

public static class GuideLoader
{
    /// <summary>
    /// Matches the stored selections against the tenant's full candidate list. The match happens
    /// here in the browser: the selections themselves are never sent to the API. Candidates that no
    /// longer exist are dropped from storage.
    /// </summary>
    public static async Task<List<GuideGroup>> LoadAsync(ApiClient api, VotingGuide guide, string slug)
    {
        var positions = await api.GetPositionsAsync(slug);
        var candidates = (await api.GetAllCandidatesAsync(slug)).ToDictionary(c => c.Id);
        var selections = await guide.GetAsync(slug);

        var valid = selections.Where(s => candidates.ContainsKey(s.CandidateId)).ToList();
        if (valid.Count != selections.Count)
        {
            await guide.ReplaceAsync(slug, valid);
        }

        var chosen = valid.Select(s => candidates[s.CandidateId]).ToList();
        return positions
            .OrderBy(p => p.Name)
            .Select(p => new GuideGroup(p, chosen.Where(c => c.PositionId == p.Id).OrderBy(c => c.Name).ToList()))
            .ToList();
    }
}
