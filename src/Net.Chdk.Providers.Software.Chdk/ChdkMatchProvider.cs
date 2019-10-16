﻿using Microsoft.Extensions.Logging;
using Net.Chdk.Adapters.Platform;
using Net.Chdk.Model.Software;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Net.Chdk.Providers.Software.Chdk
{
    public sealed class ChdkMatchProvider : MatchProvider
    {
        private static readonly Regex regex = new Regex("<a href=\"(?<path>[0-9A-Za-z_\\-./]+)\">(?<platform>[0-9a-z_]+)-(?<revision>[0-9a-z]+)-(?<version>[0-9.]+)-(?<build>[0-9]+)(-(?<buildName>[a-z]+))?(_(?<status>[A-Z]+))?.zip</a>&nbsp;&nbsp;<span class=\"kb\">\\((?<size>[0-9]+K)B\\)</span>");

        private static readonly Regex errorRegex = new Regex("<tr><td colspan=3><b><font color=red>(?<error>[^<]+)");
        
        public ChdkMatchProvider(Uri baseUri, IDictionary<string, string> buildPaths, IPlatformAdapter platformProvider, ILogger<ChdkMatchProvider> logger)
            : base(baseUri, buildPaths, platformProvider, logger)
        {
        }

        protected override MatchData? GetMatches(SoftwareCameraInfo camera, string buildName, string line)
        {
            var matches = regex.Matches(line);
            foreach (Match match in matches)
            {
                var result = GetMatches(camera, buildName, match);
                if (result != null)
                    return result;
            }

            var errorMatch = errorRegex.Match(line);
            if (errorMatch.Success)
                return new MatchData(errorMatch.Groups["error"].Value);

            return null;
        }

        protected override string ProductName => "CHDK";
    }
}