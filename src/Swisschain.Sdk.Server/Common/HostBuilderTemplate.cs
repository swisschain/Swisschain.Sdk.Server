using System.Net;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Swisschain.Sdk.Server.Common
{
    public static class HostBuilderTemplate
    {
        public static IHostBuilder SwisschainService<TStartup>(this IHostBuilder host, int restPort = 5000, int grpcPort = 5001)
            where TStartup : class
        {
            return host
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.external.json", optional: true, reloadOnChange: true);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<TStartup>();

                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.Listen(IPAddress.Any, restPort, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1;
                        });

                        options.Listen(IPAddress.Any, grpcPort, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http2;
                        });
                    });
                });
        }
    }
}