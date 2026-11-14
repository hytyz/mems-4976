using System.Linq.Expressions;
using MEMS.Data.Entities;
using MEMS.Shared.Contracts;

namespace MEMS.Data.Services;

internal static class Mapping
{
    public static readonly Expression<Func<Municipality, MunicipalityDto>> ToMunicipalityDto = m => new MunicipalityDto
    {
        Id = m.MunicipalityId,
        Name = m.Name,
        Slug = m.Slug,
        Province = m.Province,
        ElectionDate = m.ElectionDate,
        Description = m.Description,
        Tagline = m.Tagline,
        ThemeColor = m.ThemeColor,
        LogoImageId = m.LogoImageId,
        PositionCount = m.Positions.Count,
        CandidateCount = m.Candidates.Count
    };

    public static readonly Expression<Func<Position, PositionDto>> ToPositionDto = p => new PositionDto
    {
        Id = p.PositionId,
        Name = p.Name,
        SeatsAvailable = p.SeatsAvailable,
        MunicipalityId = p.MunicipalityId,
        MunicipalityName = p.Municipality!.Name,
        CandidateCount = p.Candidates.Count
    };

    public static readonly Expression<Func<Candidate, CandidateDto>> ToCandidateDto = c => new CandidateDto
    {
        Id = c.CandidateId,
        Name = c.Name,
        Profile = c.Profile,
        PhotoImageId = c.PhotoImageId,
        Website = c.Website,
        Email = c.Email,
        VideoUrl = c.VideoUrl,
        SocialX = c.SocialX,
        SocialFacebook = c.SocialFacebook,
        SocialInstagram = c.SocialInstagram,
        SocialLinkedIn = c.SocialLinkedIn,
        DateCreated = c.DateCreated,
        PositionId = c.PositionId,
        PositionName = c.Position!.Name,
        SeatsAvailable = c.Position.SeatsAvailable,
        MunicipalityId = c.MunicipalityId,
        MunicipalityName = c.Municipality!.Name,
        MunicipalitySlug = c.Municipality.Slug
    };

    public static void Apply(this Municipality entity, MunicipalityInput input)
    {
        entity.Name = input.Name;
        entity.Slug = input.Slug;
        entity.Province = input.Province;
        entity.ElectionDate = input.ElectionDate!.Value;
        entity.Description = input.Description;
        entity.Tagline = input.Tagline;
        entity.ThemeColor = input.ThemeColor;
    }

    public static void Apply(this Candidate entity, CandidateInput input)
    {
        entity.Name = input.Name;
        entity.Profile = input.Profile;
        entity.Website = input.Website;
        entity.Email = input.Email;
        entity.VideoUrl = input.VideoUrl;
        entity.SocialX = input.SocialX;
        entity.SocialFacebook = input.SocialFacebook;
        entity.SocialInstagram = input.SocialInstagram;
        entity.SocialLinkedIn = input.SocialLinkedIn;
    }
}
