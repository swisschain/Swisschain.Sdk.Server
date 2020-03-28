using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Swisschain.Sdk.Server.Authorization
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddScopeBasedAuthorization(this IServiceCollection services)
        {
            services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();
            services.AddSingleton<IAuthorizationHandler>(s => new HasScopeHandler(
                s.GetRequiredService<ILogger<HasScopeHandler>>()));

            return services;
        }
    }
}