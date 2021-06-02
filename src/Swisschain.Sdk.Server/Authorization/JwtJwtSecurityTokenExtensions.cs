using System;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Swisschain.Sdk.Server.Authorization
{
    public static class JwtJwtSecurityTokenExtensions
    {
        private static readonly JwtSecurityTokenHandler JwtSecurityTokenHandler = new JwtSecurityTokenHandler();
        public static JwtSecurityToken ReadJwtSecurityToken(this HttpContext context, ILogger logger)
        {
            if (context.Request.Headers.TryGetValue("authorization", out var value))
            {
                try
                {
                    var header = value.ToString();
                    if (header.StartsWith("Bearer "))
                    {
                        header = header.Substring("Bearer ".Length);
                        if (!JwtSecurityTokenHandler.CanReadToken(header))
                        {
                            logger.LogWarning("Bearer token '{token}' is not valid. Failed to extract username", header);
                        }
                        
                        var token = JwtSecurityTokenHandler.ReadJwtToken(header);

                        logger.LogTrace("JWT token: {token}", token);

                        return token;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to parse authorization header.");

                    return null;
                }
            }

            return null;
        }
    }
}