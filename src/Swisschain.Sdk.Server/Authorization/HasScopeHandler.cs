using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Swisschain.Sdk.Server.Authorization
{
    public class HasScopeHandler : AuthorizationHandler<HasScopeRequirement>
    {
        private readonly ILogger<HasScopeHandler> _logger;

        public HasScopeHandler(ILogger<HasScopeHandler> logger)
        {
            _logger = logger;
        }
        
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HasScopeRequirement requirement)
        {
            // Split the scopes string into an array 
            var scopes = context.User
                .FindAll(c => c.Type == "scope" && new Uri(c.Issuer) == new Uri(requirement.Issuer))
                .SelectMany(s => s.Value.Split(' '));

            // Succeed if the scope array contains the required scope
            if (scopes.Any(s => s == requirement.Scope))
            {
                context.Succeed(requirement);

                return Task.CompletedTask;
            }

            _logger.LogWarning("Authorization failed. Requirement: {@requirement} user claims: {@claims}",
                requirement,
                context.User.Claims.Select(x => new { x.Type, x.Issuer }));

            return Task.CompletedTask;
        }
    }
}