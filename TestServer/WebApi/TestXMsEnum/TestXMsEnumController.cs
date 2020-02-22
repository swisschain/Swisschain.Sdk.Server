using Microsoft.AspNetCore.Mvc;

namespace TestServer.WebApi.TestXMsEnum
{
    [Route("api/test-x-ms-enum")]
    public class TestXMsEnumController : ControllerBase
    {
        [HttpPost("{value}")]
        public AResponse Test([FromRoute] AEnum value, [FromQuery] AEnum queryStringValue, [FromBody] ARequest request)
        {
            return new AResponse();
        }
    }
}