using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Swisschain.Sdk.Server.Swagger
{
    internal class AuthorizationCheckSwaggerOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var requiredScopes = context.MethodInfo
                .GetCustomAttributes(true)
                .OfType<AuthorizeAttribute>()
                .Select(attr => attr.Policy);

            if (context.MethodInfo.DeclaringType != null)
            {
                var requiredScopesOnController = context.MethodInfo.DeclaringType
                    .GetCustomAttributes(true)
                    .OfType<AuthorizeAttribute>()
                    .Select(attr => attr.Policy);

                requiredScopes = requiredScopes.Concat(requiredScopesOnController).Distinct();
            }
            

            if (requiredScopes.Any())
            {
                operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
                operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });

                operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference {Type = ReferenceType.SecurityScheme, Id = "Bearer"},
                                Scheme = "oauth2",
                                Name = "Bearer",
                                In = ParameterLocation.Header,

                            },
                            new List<string>()
                        }
                    }
                };
            }
        }
    }

    internal class XmsEnumOperationFilter : IOperationFilter
    {
        private readonly XmsEnumExtensionsOptions _options;

        public XmsEnumOperationFilter(XmsEnumExtensionsOptions options)
        {
            _options = options;
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
            {
                return;
            }
            
            foreach (var parameter in GetEnumParameters(context))
            {
                var operationParameter = operation.Parameters.Single(p => p.Name == parameter.Name);

                XmsEnumExtensionApplicator.Apply(operationParameter.Extensions, parameter.Type, _options);
            }
        }

        private static IEnumerable<(Type Type, string Name)> GetEnumParameters(OperationFilterContext context)
        {
            return context
                .ApiDescription
                .ParameterDescriptions
                .Where(x => x.Type != null)
                .Select(x => (Type: TryGetEnumType(x), Name: x.Name))
                .Where(x => x.Type != null);
        }

        private static Type TryGetEnumType(ApiParameterDescription parameter)
        {
            if (parameter.Type.GetTypeInfo().IsEnum)
            {
                return parameter.Type;
            }

            var nullableUnderlyingType = Nullable.GetUnderlyingType(parameter.Type);

            return nullableUnderlyingType?.GetTypeInfo().IsEnum == true
                ? nullableUnderlyingType 
                : null;
        }
    }
}