using System.Linq;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace Swisschain.Sdk.Server.Logging
{
    internal class RequestIdHeaderEnricher : ILogEventEnricher
    {
        private const string RequestIdPropertyName = "RequestId";
        private readonly string _headerKey;
        private readonly IHttpContextAccessor _contextAccessor;

        public RequestIdHeaderEnricher(string headerKey)
            : this(headerKey, new HttpContextAccessor())
        {
        }

        private RequestIdHeaderEnricher(string headerKey, IHttpContextAccessor contextAccessor)
        {
            _headerKey = headerKey;
            _contextAccessor = contextAccessor;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (_contextAccessor.HttpContext == null)
            {
                return;
            }

            var requestId = GetRequestId();

            if (requestId == null)
            {
                return;
            }

            var property = new LogEventProperty(RequestIdPropertyName, new ScalarValue(requestId));

            logEvent.AddOrUpdateProperty(property);
        }

        private string GetRequestId()
        {
            return _contextAccessor.HttpContext.Request.Headers.TryGetValue(_headerKey, out var source) ? source.FirstOrDefault() : null;
        }
    }
}