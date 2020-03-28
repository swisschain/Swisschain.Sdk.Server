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
        AddJwtAuth(Config.Auth.JwtSecret);
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

### Scope-base authorization

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

Then you can use `Authorize` attribute on your controllers and action-methods to require a scope for requests execution:

```c#
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    [HttpGet)]
    [Authorize("exchange.swisschain/orders:get")]
    public async Task<IActionResult> Get()
    {
        ...
    }
    
    [HttpGet)]
    [Authorize("exchange.swisschain/orders:add")]
    public async Task<IActionResult> Add()
    {
        ...
    }
}
```