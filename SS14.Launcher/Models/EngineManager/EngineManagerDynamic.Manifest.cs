using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace SS14.Launcher.Models.EngineManager;

public sealed partial class EngineManagerDynamic
{
    // This part of the code is responsible for downloading and caching the Robust build manifest.

    private readonly SemaphoreSlim _manifestSemaphore = new(1);
    private readonly Stopwatch _manifestStopwatch = Stopwatch.StartNew();

    private Dictionary<string, VersionInfo>? _cachedRobustVersionInfo;
    private TimeSpan _robustCacheValidUntil;

    /// <summary>
    /// Look up information about an engine version.
    /// </summary>
    /// <param name="version">The version number to look up.</param>
    /// <param name="followRedirects">Follow redirections in version info.</param>
    /// <param name="cancel">Cancellation token.</param>
    /// <returns>
    /// Information about the version, or null if it could not be found.
    /// The returned version may be different than what was requested if redirects were followed.
    /// </returns>
    private async ValueTask<FoundVersionInfo?> GetVersionInfo(
        string version,
        bool followRedirects = true,
        CancellationToken cancel = default)
    {
        await _manifestSemaphore.WaitAsync(cancel);
        try
        {
            return await GetVersionInfoCore(version, followRedirects, cancel);
        }
        finally
        {
            _manifestSemaphore.Release();
        }
    }

    private async ValueTask<FoundVersionInfo?> GetVersionInfoCore(
        string version,
        bool followRedirects,
        CancellationToken cancel)
    {
        // If we have a cached copy, and it's not expired, we check it.
        if (_cachedRobustVersionInfo != null && _robustCacheValidUntil > _manifestStopwatch.Elapsed)
        {
            // Check the version. If this fails, we immediately re-request the manifest as it may have changed.
            // (Connecting to a freshly-updated server with a new Robust version, within the cache window.)
            if (FindVersionInfoInCached(version, followRedirects) is { } foundVersionInfo)
                return foundVersionInfo;
        }

        await UpdateBuildManifest(cancel);

        return FindVersionInfoInCached(version, followRedirects);
    }

    private async Task UpdateBuildManifest(CancellationToken cancel)
    {
        // TODO: If-Modified-Since and If-None-Match request conditions.

        Log.Debug("Loading manifest from {manifestUrl}...", ConfigConstants.RobustBuildsManifest);
        _cachedRobustVersionInfo =
            await ConfigConstants.RobustBuildsManifest.GetFromJsonAsync<Dictionary<string, VersionInfo>>(
                _http, cancel);

        await MergeCustomBuildManifest(cancel);

        _robustCacheValidUntil = _manifestStopwatch.Elapsed + ConfigConstants.RobustManifestCacheTime;
    }

    /// <summary>
    /// Voice chat fork: adds engine versions from <see cref="ConfigConstants.CustomRobustBuildsManifest"/>.
    /// Official versions are never replaced. Failure to load it only loses the custom versions.
    /// </summary>
    private async Task MergeCustomBuildManifest(CancellationToken cancel)
    {
        Dictionary<string, VersionInfo>? custom;
        try
        {
            Log.Debug("Loading custom manifest from {manifestUrl}...", ConfigConstants.CustomRobustBuildsManifest);
            custom = await ConfigConstants.CustomRobustBuildsManifest
                .GetFromJsonAsync<Dictionary<string, VersionInfo>>(_http, cancel);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            Log.Warning(e, "Failed to load custom engine manifest, only official engine versions are available");
            return;
        }

        if (custom == null || _cachedRobustVersionInfo == null)
            return;

        foreach (var (version, info) in custom)
        {
            if (!_cachedRobustVersionInfo.TryAdd(version, info))
                Log.Warning("Custom engine manifest tried to override official version {Version}, ignored", version);
        }
    }

    private FoundVersionInfo? FindVersionInfoInCached(string version, bool followRedirects)
    {
        Debug.Assert(_cachedRobustVersionInfo != null);

        if (!_cachedRobustVersionInfo.TryGetValue(version, out var versionInfo))
            return null;

        if (followRedirects)
        {
            while (versionInfo.RedirectVersion != null)
            {
                version = versionInfo.RedirectVersion;
                versionInfo = _cachedRobustVersionInfo[versionInfo.RedirectVersion];
            }
        }

        return new FoundVersionInfo(version, versionInfo);
    }

    private sealed record FoundVersionInfo(string Version, VersionInfo Info);

    private sealed record VersionInfo(
        bool Insecure,
        [property: JsonPropertyName("redirect")]
        string? RedirectVersion,
        Dictionary<string, BuildInfo> Platforms);

    private sealed class BuildInfo
    {
        [JsonInclude] [JsonPropertyName("url")]
        public string Url = default!;

        [JsonInclude] [JsonPropertyName("sha256")]
        public string Sha256 = default!;

        [JsonInclude] [JsonPropertyName("sig")]
        public string Signature = default!;
    }
}
