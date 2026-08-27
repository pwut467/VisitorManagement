using Microsoft.AspNetCore.Hosting;

namespace VisitorManagement.Web.Services;

public interface IPhotoStorageService
{
    Task<string?> SaveDataUrlAsync(string? dataUrl, string fileStem, CancellationToken cancellationToken = default);
    string? PublicUrl(string? relativePath);
}

public class PhotoStorageService : IPhotoStorageService
{
    private readonly IWebHostEnvironment _env;

    public PhotoStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string?> SaveDataUrlAsync(string? dataUrl, string fileStem, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dataUrl) || !dataUrl.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var comma = dataUrl.IndexOf(',');
        if (comma < 0)
        {
            return null;
        }

        var meta = dataUrl[..comma];
        var ext = meta.Contains("png", StringComparison.OrdinalIgnoreCase) ? ".png" : ".jpg";
        var bytes = Convert.FromBase64String(dataUrl[(comma + 1)..]);
        if (bytes.Length == 0 || bytes.Length > 5 * 1024 * 1024)
        {
            return null;
        }

        var relative = Path.Combine("uploads", "photos", fileStem + ext).Replace('\\', '/');
        var full = Path.Combine(_env.WebRootPath, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await File.WriteAllBytesAsync(full, bytes, cancellationToken);
        return "/" + relative;
    }

    public string? PublicUrl(string? relativePath) =>
        string.IsNullOrWhiteSpace(relativePath) ? null : relativePath;
}
