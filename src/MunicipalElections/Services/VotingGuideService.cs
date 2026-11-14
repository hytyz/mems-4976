using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MunicipalElections.Data;
using MunicipalElections.Models;

namespace MunicipalElections.Services;

public class VotingGuideService
{
    private const string SessionKey = "VotingGuide";

    private readonly IHttpContextAccessor _http;
    private readonly ApplicationDbContext _db;

    public VotingGuideService(IHttpContextAccessor http, ApplicationDbContext db)
    {
        _http = http;
        _db = db;
    }

    public bool IsAvailable => _http.HttpContext?.User.Identity?.IsAuthenticated != true;

    public List<int> GetIds()
    {
        var session = _http.HttpContext!.Session;
        string? json = session.GetString(SessionKey);
        if (string.IsNullOrEmpty(json))
        {
            return new List<int>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
        }
        catch (JsonException)
        {
            return new List<int>();
        }
    }

    public bool Contains(int candidateId) => GetIds().Contains(candidateId);

    public async Task<List<Candidate>> GetCandidatesAsync()
    {
        var ids = GetIds();
        if (ids.Count == 0)
        {
            return new List<Candidate>();
        }

        return await _db.Candidates
            .Where(c => ids.Contains(c.CandidateId))
            .Include(c => c.Position)
                .ThenInclude(p => p!.Municipality)
            .ToListAsync();
    }

    public async Task<string?> GetBlockReasonAsync(Candidate candidate)
    {
        var position = candidate.Position!;
        var selected = (await GetCandidatesAsync())
            .Where(c => c.CandidateId != candidate.CandidateId)
            .ToList();

        var other = selected.FirstOrDefault(c => c.Position!.MunicipalityId != position.MunicipalityId);
        if (other != null)
        {
            return $"Your voting guide is for {other.Position!.Municipality!.Name}. A guide covers one municipality, " +
                   $"so clear it before choosing candidates in {position.Municipality!.Name}.";
        }

        int chosen = selected.Count(c => c.PositionId == position.PositionId);
        if (chosen >= position.SeatsAvailable)
        {
            return position.SeatsAvailable == 1
                ? $"You have already chosen a candidate for {position.Name}. Only one can be elected, so remove your current choice first."
                : $"You have already chosen {chosen} candidates for {position.Name}, one for each of the {position.SeatsAvailable} seats. Remove one first.";
        }

        return null;
    }

    public void Add(int candidateId)
    {
        var ids = GetIds();
        if (!ids.Contains(candidateId))
        {
            ids.Add(candidateId);
            Save(ids);
        }
    }

    public void Remove(int candidateId)
    {
        var ids = GetIds();
        if (ids.Remove(candidateId))
        {
            Save(ids);
        }
    }

    public void Clear()
    {
        _http.HttpContext!.Session.Remove(SessionKey);
    }

    private void Save(List<int> ids)
    {
        _http.HttpContext!.Session.SetString(SessionKey, JsonSerializer.Serialize(ids));
    }
}
