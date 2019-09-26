﻿using Microsoft.Extensions.Logging;
using Net.Chdk.Meta.Generators.Platform;
using Net.Chdk.Model.Camera;
using Net.Chdk.Model.CameraModel;
using Net.Chdk.Providers.Firmware;
using Net.Chdk.Providers.Product;
using System;
using System.Collections.Generic;

namespace Net.Chdk.Providers.Substitute
{
    sealed class SubstituteProvider : ProviderResolver<ICategorySubstituteProvider>, ISubstituteProvider
    {
        private IProductProvider ProductProvider { get; }
        private IPlatformGenerator PlatformGenerator { get; }
        private IFirmwareProvider FirmwareProvider { get; }

        public SubstituteProvider(IProductProvider productProvider, IPlatformGenerator platformGenerator, IFirmwareProvider firmwareProvider, ILoggerFactory loggerFactory)
            : base(loggerFactory)
        {
            ProductProvider = productProvider;
            PlatformGenerator = platformGenerator;
            FirmwareProvider = firmwareProvider;
        }

        public IDictionary<string, string>? GetSubstitutes(CameraInfo camera, CameraModelInfo cameraModel)
        {
            var categoryName = FirmwareProvider.GetCategoryName(camera);
            if (categoryName == null)
                return null;

            var platform = GetPlatform(camera, cameraModel);
            if (platform == null)
                return null;

            var revision = FirmwareProvider.GetFirmwareRevision(camera);
            if (revision == null)
                return null;

            return GetProvider(categoryName)?.GetSubstitutes(platform, revision);
        }

        protected override IEnumerable<string> GetNames()
        {
            return ProductProvider.GetCategoryNames();
        }

        protected override ICategorySubstituteProvider CreateProvider(string categoryName) => categoryName switch
        {
            "EOS" => new EosSubstituteProvider(LoggerFactory),
            "PS" => new PsSubstituteProvider(LoggerFactory),
            _ => throw new InvalidOperationException($"Unknown category: {categoryName}"),
        };

        //TODO Move to PlatformProvider
        private string? GetPlatform(CameraInfo camera, CameraModelInfo cameraModel)
        {
            var modelId = camera?.Canon?.ModelId;
            if (modelId == null)
                return null;

            var models = cameraModel?.Names;
            if (models == null)
                return null;

            return PlatformGenerator.GetPlatform(modelId.Value, models, true);
        }
    }
}