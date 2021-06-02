using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Swisschain.Sdk.Server.WebApi.Common;

namespace Swisschain.Sdk.Server.WebApi.ExceptionsHandling
{
    public class UnhandledExceptionsMiddleware
    {
        private readonly ILogger<UnhandledExceptionsMiddleware> _logger;
        private readonly RequestDelegate _next;
        private readonly IOptions<MvcNewtonsoftJsonOptions> _jsonOptions;

        public UnhandledExceptionsMiddleware(ILogger<UnhandledExceptionsMiddleware> logger, 
            RequestDelegate next,
            IOptions<MvcNewtonsoftJsonOptions> jsonOptions)
        {
            _logger = logger;
            _next = next;
            _jsonOptions = jsonOptions;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (Exception ex)
            {
                await ErrorResponse(context, "Runtime error");

                _logger.LogError(ex, "Unhandled exception");
            }
        }

        private Task ErrorResponse(HttpContext ctx, string message)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var response = new ModelStateDictionaryErrorResponse
            {
                Errors = new Dictionary<string, string[]>
                {
                    [""] = new[] {message}
                }
            };
            
            ctx.CaptureErrorResponse(response);

            return ctx.Response.WriteAsync(JsonConvert.SerializeObject(response, _jsonOptions.Value.SerializerSettings));
        }
    }
}