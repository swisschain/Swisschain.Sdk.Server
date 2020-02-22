using System.Linq;
using System.Security.Claims;

namespace Swisschain.Sdk.Server.Authorization
{
    public static class UserExtensions
    {
        public static string GetTenantId(this ClaimsPrincipal user)
        {
            return user.Identities
                .SelectMany(x => x.Claims)
                .Where(c => c.Type == "tenant-id")
                .Select(x => x.Value)
                .SingleOrDefault();
        }
    }
}