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
            
            var nameSegments = (name?.Name ?? string.Empty).Split('.', StringSplitOptions.RemoveEmptyEntries);

            if (nameSegments.Length > 2)
            {
                AppName = string.Join('.', nameSegments.Skip(1));
            }

            AppVersion = name?.Version.ToString();
        }

        public static DateTime StartedAt { get; }

        public static string AppName { get; }

        public static string AppVersion { get; }
    }
}