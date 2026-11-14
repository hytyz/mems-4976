namespace MunicipalElections.Services;

public class UploadStorage
{
    public const string RequestPath = "/uploads";

    public UploadStorage(string rootPath)
    {
        RootPath = rootPath;
        Directory.CreateDirectory(rootPath);
    }

    public string RootPath { get; }

    public static string ResolveRootPath(IConfiguration configuration, IHostEnvironment env)
    {
        string? configured = configuration["Storage:UploadsPath"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(configured, env.ContentRootPath);
        }

        string? home = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")) && !string.IsNullOrEmpty(home))
        {
            return Path.Combine(home, "data", "uploads");
        }

        return Path.Combine(env.ContentRootPath, "App_Data", "uploads");
    }
}

public class ImageFileService
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

    private readonly UploadStorage _storage;

    public ImageFileService(UploadStorage storage)
    {
        _storage = storage;
    }

    public string? Save(IFormFile? file, string subfolder, out string? error)
    {
        error = null;
        if (file is null || file.Length == 0)
        {
            return null;
        }

        if (file.Length > MaxBytes)
        {
            error = "Image must be 5 MB or smaller.";
            return null;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            error = "Only JPG, JPEG, PNG, GIF and WEBP images are allowed.";
            return null;
        }

        using var input = file.OpenReadStream();
        string? detectedExtension = DetectImageExtension(input);
        if (detectedExtension is null)
        {
            error = "The uploaded file is not a valid JPG, PNG, GIF or WEBP image.";
            return null;
        }

        bool extensionMatches = detectedExtension == extension ||
                                (detectedExtension == ".jpg" && extension == ".jpeg");
        if (!extensionMatches)
        {
            error = "The contents of the file do not match its extension.";
            return null;
        }

        string fileName = $"{Guid.NewGuid():N}{detectedExtension}";
        string folder = Path.Combine(_storage.RootPath, subfolder);
        Directory.CreateDirectory(folder);

        input.Position = 0;
        using var output = new FileStream(Path.Combine(folder, fileName), FileMode.CreateNew);
        input.CopyTo(output);

        return fileName;
    }

    public void Delete(string? fileName, string subfolder)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        string filePath = Path.Combine(_storage.RootPath, subfolder, Path.GetFileName(fileName));
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private static string? DetectImageExtension(Stream stream)
    {
        byte[] header = new byte[12];
        int read = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
        ReadOnlySpan<byte> bytes = header.AsSpan(0, read);

        if (bytes.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }))
        {
            return ".jpg";
        }

        if (bytes.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return ".png";
        }

        if (bytes.StartsWith("GIF87a"u8) || bytes.StartsWith("GIF89a"u8))
        {
            return ".gif";
        }

        if (bytes.Length >= 12 && bytes.StartsWith("RIFF"u8) && bytes.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return ".webp";
        }

        return null;
    }
}
