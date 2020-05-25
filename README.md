# Swisschain.Sdk.Server

SDK for HTTP and gRPC services.

[![nuget](https://img.shields.io/nuget/v/Swisschain.Sdk.Server?color=green&label=nuget%3A%20Swisschain.Sdk.Server)](https://www.nuget.org/packages/Swisschain.Sdk.Server/)

## Logging

Specify `HOSTNAME` environment variable on your local machine to distinguish your local logs from the rest of the logs in the centralized Seq instance.
If `HOSTNAME` is empty, OS user name will be used instead.

Specify `SeqUrl` with [Seq](https://datalust.co/seq) url in the json settings or in the environment variables to forward logs to the Seq.

Specify `ConsoleOutputLogLevel` with `Error` or `Warning` in the json settings or in the environment variables to disable standart output in console.

Specify `RemoteSettingsReadTimeout` as a [TimeSpan](https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-timespan-format-strings) to override default remote settings reading time-out. Default timeout is 5 seconds.

Specify `RemoteSettingsRequired` with `true` to make remote settings mandatory. By default the remote settings are optional.

Specify `ElasticsearchLogs.IndexPrefixName` with the index name preffix in the json settings or in the environment variables to use specific index name Elasticsearch. By default IndexPrefixName = `log`.

Specify `ElasticsearchLogs.NodeUrls` with the URL addresses of Elasticsearch nodes in the json settings or in the environment variables to write logs to Elasticsearch.
```
{
	"ElasticsearchLogs": {
		"NodeUrls": ["http://elasticsearch-1.elk-logs:9200", "http://elasticsearch-2.elk-logs:9200", "http://elasticsearch-3.elk-logs:9200"],
		"IndexPrefixName": "logs"
	}
}
```
```
ElasticsearchLogs__NodeUrls__1 = "http://elasticsearch-1.elk-logs:9200"
ElasticsearchLogs__NodeUrls__2 = "http://elasticsearch-2.elk-logs:9200"
ElasticsearchLogs__NodeUrls__3 = "http://elasticsearch-3.elk-logs:9200"
```

Specify `Serilog.minimumLevel` with [Serilog](https://github.com/serilog/serilog-settings-configuration) config in the json settings or in the environment variables to manage log level.
```
{
	"Serilog": {
		"minimumLevel": {
			"default": "Information",
			"override": {
				"Microsoft": "Warning",
				"System": "Warning"
			}
		}
	  }
}
```

`Verbose`:	Verbose is the noisiest level, rarely (if ever) enabled for a production app.
`Debug`:	Debug is used for internal system events that are not necessarily observable from the outside, but useful when determining how something happened.
`Information`:	Information events describe things happening in the system that correspond to its responsibilities and functions. Generally these are the observable actions the system can perform.
`Warning`:	When service is degraded, endangered, or may be behaving outside of its expected parameters, Warning level events are used.
`Error`:	When functionality is unavailable or expectations broken, an Error event is used.
`Fatal`:	The most critical level, Fatal events demand immediate attention.





## Auth

### JWT authentication

If you want to add JWT-based Bearer authentication for HTTP endpoints in your service, add `AddJwtAuth` call to your `Startup` constructor:

```c#
public class Startup : SwisschainStartup<AppConfig>
{
    public Startup(IConfiguration config) : base(config)
    {
        AddJwtAuth(Config.Auth.JwtSecret, "exchange.swisschain.io");
    }
}
```

If your JWT token contains `tenant-id` claim, you can use `GetTenantId` extension method to get current tenant ID of the HTTP request in your controllers:

```c#
[Route("api/who-am-i")]
public class WhoAmIController : ControllerBase
{
    [HttpGet)]
    public string Get()
    {
        var tenantId = User.GetTenantId();
        
        return tenantId;
    }
}
```

Your JWT token SHOULD containt:

* `exp` claim [RFC-7519](https://tools.ietf.org/html/rfc7519#section-4.1.4)
* `aud` claim [RFC-7519](https://tools.ietf.org/html/rfc7519#section-4.1.3). JWT token should contain an audience specified in the `AddJwtAuth` call. 
It can contain an array of the audience still one of which is required by your service.

#### Debugging and developing

For the developing and debugging purposes you can get JWT token using [jwt.io](https://jwt.io). Just put all required claims to the payload and the same secret as you've specified in
`AddJwtAuth` call in your service.

### Scope-based authorization

If you want to add scope-based authorization for HTTP endpoints in your service, add `services.AddScopeBasedAuthorization` call to your `Startup.ConfigureServicesExt`:

```c#
public class Startup : SwisschainStartup<AppConfig>
{
    ...
    
    public override void ConfigureServicesExt(IServiceCollection services)
    {
        services.AddScopeBasedAuthorization();
    }
}
```

Then you can use `Authorize` attribute on your controllers and action-methods to require scope for requests execution:

```c#
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    [HttpGet)]
    [Authorize("exchange.swisschain.io/orders:get")]
    public async Task<IActionResult> Get()
    {
        ...
    }
    
    [HttpGet)]
    [Authorize("exchange.swisschain.io/orders:add")]
    public async Task<IActionResult> Add()
    {
        ...
    }
}
```

Your JWT token should contain the scope ([rfc8693](https://tools.ietf.org/html/rfc8693)) specified on your controller or action method. You can use `Authorize` attribute without any scope still just to require the request to be authorized.

#### Scopes format

`<api-name>.swisschain.io/<resources>:<operation>`. You can use as granular scopes for your service as you need. For instance, you can omit operation, resource or event api-name,  if you don't need it and make scopes more granular later on.
