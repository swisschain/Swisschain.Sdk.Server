using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Swisschain.Sdk.Server.WebApi.ExceptionsHandling
{
    public class ErrorResponseActionFilter : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Result is IStatusCodeActionResult statusCodeResult)
            {
                if (statusCodeResult.StatusCode != null &&
                    (statusCodeResult.StatusCode < 200 || statusCodeResult.StatusCode >= 300))
                {
                    context.Result = CreateValidationResult(context.ModelState, statusCodeResult.StatusCode.Value);
                }
            }
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                context.Result = CreateValidationResult(context.ModelState, StatusCodes.Status400BadRequest);
            }
        }

        private static IActionResult CreateValidationResult(ModelStateDictionary modelState, int statusCode)
        {
            var resultObject = new
            {
                errors = modelState.Count != 0
                    ? modelState
                        .Select(x => new
                        {
                            Key = FormatFieldName(x.Key),
                            Messages = x.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        })
                        .Where(x => x.Messages.Any())
                        .ToDictionary(
                            x => x.Key,
                            x => x.Messages)
                    : new Dictionary<string, string[]> {{"", new[] {"Unknown error"}}}
            };

            return new ObjectResult(resultObject) {StatusCode = statusCode};
        }

        private static string FormatFieldName(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                return fieldName;
            }

            // Keeps kebab-case field names as is
            if (fieldName.IndexOf('-') != -1)
            {
                return fieldName;
            }

            // Converts PascalCase field names to the camelCase
            if (fieldName.Length > 1)
            {
                return char.ToLowerInvariant(fieldName[0]) + fieldName.Substring(1);
            }

            return fieldName.ToLowerInvariant();
        }
    }
}