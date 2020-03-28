# Swisschain.Sdk.Server

SDK for HTTP and gRPC services.

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