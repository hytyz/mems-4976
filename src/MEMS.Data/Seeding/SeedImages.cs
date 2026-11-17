using MEMS.Data.Entities;
using MEMS.Data.Services;

namespace MEMS.Data.Seeding;

/// <summary>Seed logos and candidate portraits, embedded from Seeding/Images.</summary>
internal static class SeedImages
{
    private const string ResourcePrefix = "seed-images/";

    /// <summary>Returns a validated image row owned by the municipality, or null when no seed image has that name.</summary>
    public static StoredImage? Create(string fileName, int municipalityId)
    {
        using var stream = typeof(SeedImages).Assembly.GetManifestResourceStream(ResourcePrefix + fileName);
        if (stream is null)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        byte[] content = buffer.ToArray();

        if (ImageInspector.Validate(content, out string contentType) is { } error)
        {
            throw new InvalidOperationException($"Seed image '{fileName}' is invalid: {error}");
        }

        return ImageInspector.ToEntity(content, contentType, municipalityId);
    }
}
