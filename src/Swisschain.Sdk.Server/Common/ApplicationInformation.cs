using System;
using System.Linq;
using System.Reflection;

namespace Swisschain.Sdk.Server.Common
{
    public static class ApplicationInformation
    {
        static ApplicationInformation()
        {
            StartedAt = DateTime.UtcNow;

            var name = Assembly.GetEntryAssembly()?.GetName();

            string appName = name?.Name ?? string.Empty;

            var nameSegments = appName.Split('.', StringSplitOptions.RemoveEmptyEntries);

            if (nameSegments.Length > 2)
            {
                appName = string.Join('.', nameSegments.Skip(1));
            }

            AppName = appName;
            AppVersion = name?.Version?.ToString();
        }

        public static DateTime StartedAt { get; }

        public static string AppName { get; }

        public static string AppVersion { get; }
    }
}
