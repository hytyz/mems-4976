using MEMS.Data.Entities;
using MEMS.Data.Tenancy;
using MEMS.Shared;
using Microsoft.EntityFrameworkCore;

namespace MEMS.Data.Services;

public sealed record ImageContent(string ContentType, byte[] Data);

/// <summary>Identifies an image by its file signature. The file name and browser content type are never trusted.</summary>
public static class ImageInspector
{
    public static string? DetectContentType(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }))
        {
            return "image/jpeg";
        }

        if (bytes.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return "image/png";
        }

        if (bytes.StartsWith("GIF87a"u8) || bytes.StartsWith("GIF89a"u8))
        {
            return "image/gif";
        }

        if (bytes.Length >= 12 && bytes.StartsWith("RIFF"u8) && bytes.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return "image/webp";
        }

        return null;
    }

    /// <summary>Returns an error message, or null when the bytes are an acceptable image.</summary>
    public static string? Validate(byte[]? content, out string contentType)
    {
        contentType = string.Empty;

        if (content is null || content.Length == 0)
        {
            return "Choose an image file to upload.";
        }

        if (content.LongLength > ImageRules.MaxBytes)
        {
            return $"The image must be {ImageRules.MaxSizeText} or smaller.";
        }

        string? detected = DetectContentType(content);
        if (detected is null)
        {
            return "The file is not a valid JPG, PNG, GIF or WEBP image.";
        }

        contentType = detected;
        return null;
    }

    internal static StoredImage ToEntity(byte[] content, string contentType, int municipalityId) => new()
    {
        StoredImageId = Guid.NewGuid(),
        ContentType = contentType,
        Data = content,
        Length = content.Length,
        UploadedUtc = DateTime.UtcNow,
        MunicipalityId = municipalityId
    };
}

public interface IImageService
{
    /// <summary>The image, or null when it does not exist inside the current tenant boundary.</summary>
    Task<ImageContent?> GetAsync(Guid imageId, CancellationToken cancellationToken = default);
}

public sealed class ImageService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ITenantContextInitializer tenantInitializer) : IImageService
{
    public async Task<ImageContent?> GetAsync(Guid imageId, CancellationToken cancellationToken = default)
    {
        await tenantInitializer.EnsureInitializedAsync(cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.Images
            .AsNoTracking()
            .Where(i => i.StoredImageId == imageId)
            .Select(i => new ImageContent(i.ContentType, i.Data))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
