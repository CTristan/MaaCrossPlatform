using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MaaGui.Helper;
using Serilog;

namespace MaaGui.Services.Web;

/// <summary>
/// Maa API service implementation
/// </summary>
public class MaaApiService : IMaaApiService
{
    private readonly IHttpService _httpService;
    private const string MaaApiBase = "https://api.maa.plus/";

    public MaaApiService(IHttpService httpService)
    {
        _httpService = httpService;
    }

    public async Task<string> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        return await _httpService.GetAsync(MaaApiBase + "version", cancellationToken);
    }

    public async Task<ResourceUpdateInfo> GetResourceUpdateAsync(CancellationToken cancellationToken = default)
    {
        var json = await _httpService.GetAsync(MaaApiBase + "resource/update", cancellationToken);
        return JsonSerializer.Deserialize<ResourceUpdateInfo>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
    }

    public async Task<string> DownloadResourceAsync(string url, string destination, CancellationToken cancellationToken = default)
    {
        var data = await _httpService.DownloadAsync(url, cancellationToken);
        await File.WriteAllBytesAsync(destination, data, cancellationToken);
        return destination;
    }

    public async Task<JsonNode?> RequestMaaApiWithCache(string url, bool force = false)
    {
        try
        {
            if (!force)
            {
                var cached = LoadApiCache(url);
                if (cached != null) return cached;
            }

            var fullUrl = url.StartsWith("http") ? url : MaaApiBase + url;
            var json = await _httpService.GetAsync(fullUrl);
            var jobj = JsonNode.Parse(json);

            // Save to cache
            SaveApiCache(url, json);

            return jobj;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to request Maa API with cache: {Url}", url);
            return LoadApiCache(url);
        }
    }

    public JsonNode? LoadApiCache(string url)
    {
        try
        {
            var cachePath = GetCachePath(url);
            if (File.Exists(cachePath))
            {
                var json = File.ReadAllText(cachePath);
                return JsonNode.Parse(json);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load API cache: {Url}", url);
        }
        return null;
    }

    private void SaveApiCache(string url, string json)
    {
        try
        {
            var cachePath = GetCachePath(url);
            var dir = Path.GetDirectoryName(cachePath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(cachePath, json);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save API cache: {Url}", url);
        }
    }

    private string GetCachePath(string url)
    {
        var safeName = url.Replace("/", "_").Replace(":", "_").Replace("?", "_");
        return Path.Combine(PathsHelper.CacheDir, "api", safeName);
    }
}
