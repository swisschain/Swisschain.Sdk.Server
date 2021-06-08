using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Serilog;
using StackExchange.Utils;
using Swisschain.Sdk.Server.Common;
using Swisschain.Sdk.Server.Configuration.WebJsonSettings;

namespace Swisschain.Sdk.Server.Configuration
{
    internal static class SwisschainConfigurationBuilder
    {
        public static IConfigurationBuilder AddSwisschainConfiguration(this IConfigurationBuilder configBuilder, WebJsonConfigurationSourcesBuilder webJsonConfigurationBuilder,
            FileJsonConfigurationLocation locations)
        {
            if (locations != null)
            {
                Validate(locations);
            }
            
            locations ??= FileJsonConfigurationLocation.BindDefault();
          
            configBuilder.WithPrefix(
                    "secrets",
                    c =>
                    {
                        foreach (var path in locations.SecretFilePaths)
                        {
                            c.AddJsonFile(path, optional: false, reloadOnChange: true);
                        }
                    }
                )
                .WithSubstitution(
                    c =>
                    {
                        foreach (var path in locations.SettingFilePaths)
                        {
                            c.AddJsonFile(path, optional: false, reloadOnChange: true);
                        }
                        
                        foreach (var source in webJsonConfigurationBuilder.Sources)
                        {
                            c.AddWebJsonConfiguration(WebJsonHttpClientProvider.DefaultClient, source.Url,
                                isOptional: source.IsOptional);
                        }

                        c.AddJsonFile("appsettings.json", optional: true)
                            .AddJsonFile($"appsettings.{ApplicationEnvironment.Environment}.json", optional: true)
                            .AddEnvironmentVariables();

                         if (locations.ShouldLogSettings)
                        {
                            Log.Logger.Information("Settings provided : {@Settings}", c.Build().GetDebugView());
                        }
                    }
                );
            
            return configBuilder;
        }

        public static IConfigurationRoot ValidateSubstitutions(this IConfigurationRoot configurationRoot)
        {
            // matches a key wrapped in braces and prefixed with a '$' 
            // e.g. ${secrets:Key} or ${secrets:Section:Key} or ${secrets:Section:NestedSection:Key}
            var substitutionPattern = new Regex(@"\$\{secrets:(?<key>[^\s]+?)\}", RegexOptions.Compiled);

            var errors = new Dictionary<string, string>();
            foreach (var kv in configurationRoot.AsEnumerable())
            {
                if (kv.Value != null && substitutionPattern.Matches(kv.Value).Any())
                {
                    errors.Add(kv.Key, kv.Value);
                }
            }

            if (errors.Any())
            {
                var errorsDesc = string.Join(", ", errors.Select(p => $"{p.Key} : [{p.Value}]"));
                throw new InvalidOperationException($"Configuration mismatch: substitutions not found for {errorsDesc}");
            }

            return configurationRoot;
        }

        private static void Validate(FileJsonConfigurationLocation locations)
        {
            if (locations.SettingFilePaths == null)
            {
                throw new ArgumentNullException(nameof(locations.SettingFilePaths));
            }

            if (locations.SecretFilePaths == null)
            {
                throw new ArgumentNullException(nameof(locations.SecretFilePaths));
            }

            if (!locations.SecretFilePaths.Any())
            {
                throw new ArgumentException("Provided sequence is empty", nameof(locations.SecretFilePaths));
            }

            if (!locations.SettingFilePaths.Any())
            {
                throw new ArgumentException("Provided sequence is empty", nameof(locations.SettingFilePaths));
            }
        }
    }
}