using Microsoft.AspNetCore.Http;

namespace Swisschain.Sdk.Server.WebApi.ExceptionsHandling
{
    internal static class ErrorResponseExtensions
    {
        private const string ErrorResponseKey = "ErrorResponse";

        public static void CaptureErrorResponse(this HttpContext httpContext, object error)
        {
            httpContext.Items[ErrorResponseKey] = error;
        }

        public static object GetErrorResponse(this HttpContext httpContext)
        {
            return httpContext.Items[ErrorResponseKey];
        }
    }
}