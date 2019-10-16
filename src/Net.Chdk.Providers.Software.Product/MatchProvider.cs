﻿using Microsoft.Extensions.Logging;
using Net.Chdk.Adapters.Platform;
using Net.Chdk.Model.Software;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Net.Chdk.Providers.Software
{
    public abstract class MatchProvider : IMatchProvider<MatchData>
    {
        private ILogger Logger { get; }

        protected Uri BaseUri { get; }
        protected IDictionary<string, string> BuildPaths { get; }

        private IPlatformAdapter PlatformAdapter { get; }

        private readonly List<string> platforms;
        private readonly List<string> revisions;
        private readonly List<string> builds;

        protected MatchProvider(Uri baseUri, IDictionary<string, string> buildPaths, IPlatformAdapter platformAdapter, ILogger logger)
        {
            Logger = logger;
            BaseUri = baseUri;
            BuildPaths = buildPaths;
            PlatformAdapter = platformAdapter;

            platforms = new List<string>();
            revisions = new List<string>();
            builds = new List<string>();
        }

        public async Task<MatchData> GetMatchesAsync(SoftwareInfo software, string buildName, CancellationToken cancellationToken)
        {
            if (software?.Camera == null)
                throw new InvalidOperationException("Null camera");

            var buildUri = GetBuildUri(buildName);

            Logger.LogTrace("Fetching {0}", buildUri);

            using (var http = new HttpClient())
            using (var resp = await http.GetAsync(buildUri, cancellationToken))
            {
                try
                {
                    resp.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    Logger.LogError(0, ex, "Error fetching");
                    throw;
                }
                using (var stream = await resp.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(stream))
                {
                    return await GetMatchesAsync(software.Camera, buildName, reader);
                }
            }
        }

        private MatchData GetError()
        {
            if (this.platforms.Count == 0)
                return new MatchData("Download_InvalidFormat_Text");

            var platforms = GetItems(this.platforms);
            var revisions = GetItems(this.revisions);
            var builds = GetItems(this.builds);
            return new MatchData(platforms, revisions, builds);
        }

        private async Task<MatchData> GetMatchesAsync(SoftwareCameraInfo camera, string buildName, TextReader reader)
        {
            platforms.Clear();
            revisions.Clear();
            builds.Clear();

            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var matches = GetMatches(camera, buildName, line);
                if (matches != null)
                    return matches;
            }

            return GetError();
        }

        protected abstract MatchData? GetMatches(SoftwareCameraInfo camera, string buildName, string line);

        protected abstract string ProductName { get; }

        protected MatchData? GetMatches(SoftwareCameraInfo camera, string buildName, Match match)
        {
            var platform = NormalizePlatform(match.Groups["platform"].Value);
            if (platform.Equals(camera.Platform))
            {
                var matches = GetPlatformMatches(camera, buildName, match);
                if (matches != null)
                    return matches;
            }
            platforms.Add(platform);
            return null;
        }

        private MatchData? GetPlatformMatches(SoftwareCameraInfo camera, string buildName, Match match)
        {
            var revision = match.Groups["revision"].Value;
            if (revision.Equals(camera.Revision))
            {
                var matches = GetRevisionMatches(camera, buildName, match);
                if (matches != null)
                    return matches;
            }
            revisions.Add(revision);
            return null;
        }

        private MatchData? GetRevisionMatches(SoftwareCameraInfo _, string buildName, Match match)
        {
            var build = match.Groups["buildName"].Value;
            if (build.Equals(buildName))
            {
                var matches = GetMatches(buildName, match);
                if (matches != null)
                    return matches;
            }
            builds.Add(build);
            return null;
        }

        protected virtual MatchData GetMatches(string buildName, Match match)
        {
            return new MatchData(match);
        }

        private Uri GetBuildUri(string buildName)
        {
            string? buildPath = null;
            BuildPaths?.TryGetValue(buildName, out buildPath);
            if (string.IsNullOrEmpty(buildPath))
                return BaseUri;
            var uriBuilder = new UriBuilder(BaseUri);
            uriBuilder.Path += buildPath;
            return uriBuilder.Uri;
        }

        private string NormalizePlatform(string platform)
        {
            return PlatformAdapter.NormalizePlatform(ProductName, platform);
        }

        private static IEnumerable<string>? GetItems(List<string> items)
        {
            return items?.Count > 0
                ? items.Distinct()
                : null;
        }
    }
}
