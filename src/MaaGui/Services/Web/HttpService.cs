using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace MaaGui.Services.Web;

/// <summary>
/// HTTP service implementation
/// </summary>
public class HttpService : IHttpService, IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed = false;

    private const string UserAgent = "MaaAssistantArknights/Avalonia";

    public HttpService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
    }

    public async Task<string> GetAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            Log.Debug("HTTP GET: {Url}", url);
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            Log.Debug("HTTP GET response: {Url} - {Length} bytes", url, content.Length);

            return content;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "HTTP GET failed: {Url}", url);
            throw;
        }
    }

    public async Task<byte[]> DownloadAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            Log.Debug("HTTP Download: {Url}", url);
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsByteArrayAsync();
            Log.Debug("HTTP Download complete: {Url} - {Length} bytes", url, content.Length);

            return content;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "HTTP Download failed: {Url}", url);
            throw;
        }
    }

    public HttpClient GetClient()
    {
        return _httpClient;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
