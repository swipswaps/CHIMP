﻿using Microsoft.Extensions.Logging;
using Net.Chdk.Adapters.Platform;
using Net.Chdk.Model.Software;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Net.Chdk.Providers.Software.Chdk
{
    public sealed class ChdkDeMatchProvider : MatchProvider
    {
        private static readonly Regex regex = new Regex("^(?<path>CHDK_DE_(?<platform>[0-9a-z_]+)-(?<revision>[0-9a-z]+)-(?<version>[0-9.]+)-(?<build>[0-9]+)(-(?<buildName>[a-z]+))?(_(?<status>[A-Z]+))?.zip)$");

        public ChdkDeMatchProvider(Uri baseUri, IDictionary<string, string> buildPaths, IPlatformAdapter platformAdapter, ILogger<ChdkDeMatchProvider> logger)
            : base(baseUri, buildPaths, platformAdapter, logger)
        {
        }

        protected override MatchData? GetMatches(SoftwareCameraInfo camera, string buildName, string line)
        {
            var match = regex.Match(line);
            if (match.Success)
            {
                var result = GetMatches(camera, buildName, match);
                if (result != null)
                    return result;
            }
            return null;
        }

        protected override string ProductName => "CHDK";
    }
}