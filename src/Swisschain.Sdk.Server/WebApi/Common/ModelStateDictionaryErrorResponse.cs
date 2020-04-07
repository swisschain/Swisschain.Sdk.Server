using System.Collections.Generic;

namespace Swisschain.Sdk.Server.WebApi.Common
{
    public class ModelStateDictionaryErrorResponse
    {
        public Dictionary<string, string[]> Errors { get; set; }
    }
}
