using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace MaaGui.Services.Web;

/// <summary>
/// Maa API service interface
/// </summary>
public interface IMaaApiService
{
    /// <summary>
    /// Get version info from API
    /// </summary>
    Task<string> GetVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get resource update info
    /// </summary>
    Task<ResourceUpdateInfo> GetResourceUpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Download resource package
    /// </summary>
    Task<string> DownloadResourceAsync(string url, string destination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Request Maa API with cache (Legacy support)
    /// </summary>
    Task<JsonNode?> RequestMaaApiWithCache(string url, bool force = false);

    /// <summary>
    /// Load API cache (Legacy support)
    /// </summary>
    JsonNode? LoadApiCache(string url);
}

/// <summary>
/// Resource update information
/// </summary>
public class ResourceUpdateInfo
{
    public string Version { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public List<ResourceFile> Files { get; set; } = new();
}

/// <summary>
/// Resource file information
/// </summary>
public class ResourceFile
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Hash { get; set; } = string.Empty;
}
