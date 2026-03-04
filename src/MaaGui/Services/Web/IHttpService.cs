using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MaaGui.Services.Web;

/// <summary>
/// HTTP service interface
/// </summary>
public interface IHttpService
{
    /// <summary>
    /// Get content from URL
    /// </summary>
    Task<string> GetAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download file from URL
    /// </summary>
    Task<byte[]> DownloadAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get HTTP client
    /// </summary>
    HttpClient GetClient();
}
