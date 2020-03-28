﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Swisschain.Sdk.Server.Swagger
{
    public static class SwaggerGenOptionsExtensions
    {
        /// <summary>
        /// Enables "x-ms-enum" swagger extension, which allows Autorest tool generates enum or set of string constants for each server-side enum.
        /// </summary>
        /// <param name="swaggerOptions"></param>
        /// <param name="options">"x-ms-enum" extensions options. Default value is <see cref="XmsEnumExtensionsOptions.UseEnums"/></param>
        public static void EnableXmsEnumExtension(this SwaggerGenOptions swaggerOptions, XmsEnumExtensionsOptions options = XmsEnumExtensionsOptions.UseEnums)
        {
            swaggerOptions.ParameterFilter<XmsEnumParameterFilter>(options);
            swaggerOptions.SchemaFilter<XmsEnumSchemaFilter>(options);
            swaggerOptions.OperationFilter<XmsEnumOperationFilter>(options);
        }

        /// <summary>
        /// Makes response value types to be not nullable, when client code is generated by autorest
        /// </summary>
        public static void MakeResponseValueTypesRequired(this SwaggerGenOptions swaggerOptions)
        {
            swaggerOptions.SchemaFilter<ResponseValueTypesRequiredSchemaFilter>();
        }

        public static void AddJwtBearerAuthorization(this SwaggerGenOptions swaggerOptions)
        {
            swaggerOptions.SwaggerGeneratorOptions.OperationFilters.Add(new AuthorizationCheckSwaggerOperationFilter());
            swaggerOptions.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });
        }
    }
}