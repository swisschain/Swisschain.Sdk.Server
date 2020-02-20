using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;

namespace Swisschain.Sdk.Server.Swagger
{
    internal static class XmsEnumExtensionApplicator
    {
        public static void Apply(IDictionary<string, IOpenApiExtension> extensions, Type enumType, XmsEnumExtensionsOptions options)
        {
            var name = enumType.GetTypeInfo().IsGenericType && enumType.GetGenericTypeDefinition() == typeof(Nullable<>)
                ? enumType.GetGenericArguments()[0].Name
                : enumType.Name;
            var modelAsString = options != XmsEnumExtensionsOptions.UseEnums;

            extensions.Add("x-ms-enum", new OpenApiObject
            {
                ["name"] = new OpenApiString(name),
                ["modelAsString"] = new OpenApiBoolean(modelAsString)
            });
        }
    }
}