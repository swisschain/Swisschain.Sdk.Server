﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using StackExchange.Utils;

namespace Swisschain.Sdk.Server.Configuration.FileJsonSettings
{
    public static class ConfigurationBuilderExtensions
    {
        public static void AddFilesJsonConfiguration(this IConfigurationBuilder cfg)
        {
            var locations = FileJsonSettingsLocations.Bind();

            if (!locations.Provided)
            {
                return;
            }
            
            cfg.WithPrefix(
                    "secrets",
                    c =>
                    {
                        foreach (var path in locations.SecretsFilePath)
                        {
                            c.AddJsonFile(path, optional: false, reloadOnChange: true);
                        }
                    }
                )
                .WithSubstitution(
                    c =>
                    {
                        foreach (var path in locations.SettingsFilePath)
                        {
                            c.AddJsonFile(path, optional: false, reloadOnChange: true);
                        }
                    }
                );
                    
            // matches a key wrapped in braces and prefixed with a '$' 
            // e.g. ${Key} or ${Section:Key} or ${Section:NestedSection:Key}
            var substitutionPattern = new Regex(@"\$\{(?<key>[^\s]+?)\}", RegexOptions.Compiled);

            foreach (var kv in cfg.Build().AsEnumerable())
            {
                if (kv.Value != null && substitutionPattern.Matches(kv.Value).Any())
                {
                    throw new InvalidOperationException(
                        $"Configuration mismatch: secret value {kv.Value} not substituted for {kv.Key}");
                }
            }
        }
    }
}