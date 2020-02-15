using System;
using System.Reflection;

namespace Swisschain.Sdk.Server.Common
{
    public static class ApplicationInformation
    {
        public static DateTime StartedAt { get; }

        public static string AppName { get; }

        public static string AppVersion { get; }

        static ApplicationInformation()
        {
            StartedAt = DateTime.UtcNow;
            var name = Assembly.GetEntryAssembly()?.GetName();
            AppName = name.Name;
            AppVersion = name.Version.ToString();
        }
    }
}