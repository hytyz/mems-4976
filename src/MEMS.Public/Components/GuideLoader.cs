using MEMS.Public.Services;
using MEMS.Shared.Contracts;

namespace MEMS.Public.Components;

public sealed record GuideGroup(PositionDto Position, List<CandidateDto> Candidates);

public static class GuideLoader
{
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
