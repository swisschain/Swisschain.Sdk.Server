using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Hosting;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swisschain.Sdk.Server.Authorization;
using Swisschain.Sdk.Server.Logging;
using Swisschain.Sdk.Server.Swagger;
using Swisschain.Sdk.Server.WebApi.ExceptionsHandling;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Swisschain.Sdk.Server.Common
{
    public class SwisschainStartup<TAppSettings>
        where TAppSettings : class
    {
        private bool _useJwtAuth;
        private string _jwtSecret;
        private string _jwtAudience;
        
        public SwisschainStartup(IConfiguration configRoot)
        {
            ConfigRoot = configRoot;
            Config = ConfigRoot.Get<TAppSettings>();
            ExceptionHandlingMiddlewares = new List<(Type, object[])>();
            AfterAuthHandlingMiddlewares = new List<(Type, object[])>();
            AfterRoutingHandlingMiddlewares = new List<(Type Type, object[] Args)>();
            ModelStateDictionaryResponseCodes = new HashSet<int>();

            AddExceptionHandlingMiddleware<UnhandledExceptionsMiddleware>();

            ModelStateDictionaryResponseCodes.Add(StatusCodes.Status400BadRequest);
            ModelStateDictionaryResponseCodes.Add(StatusCodes.Status500InternalServerError);
        }

        public IConfiguration ConfigRoot { get; }

        public TAppSettings Config { get; }

        public List<(Type Type, object[] Args)> ExceptionHandlingMiddlewares { get; }

        public List<(Type Type, object[] Args)> AfterAuthHandlingMiddlewares { get; }

        public List<(Type Type, object[] Args)> AfterRoutingHandlingMiddlewares { get; }

        public ISet<int> ModelStateDictionaryResponseCodes { get; }

        protected void AddJwtAuth(string secret, string audience)
        {
            _useJwtAuth = true;
            _jwtSecret = secret;
            _jwtAudience = audience;
        }

        protected void AddExceptionHandlingMiddleware<TMiddleware>(params object[] args)
        {
            ExceptionHandlingMiddlewares.Add((typeof(TMiddleware), args));
        }

        protected void AddAfterAuthHandlingMiddlewares<TMiddleware>(params object[] args)
        {
            AfterAuthHandlingMiddlewares.Add((typeof(TMiddleware), args));
        }

        protected void AddAfterRoutingHandlingMiddlewares<TMiddleware>(params object[] args)
        {
            AfterRoutingHandlingMiddlewares.Add((typeof(TMiddleware), args));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers(ConfigureMvcOptions)
                .AddNewtonsoftJson(ConfigureMvcNewtonsoftJsonOptions);

            services.Configure<ApiBehaviorOptions>(options =>
            {
                // ErrorResponseActionFilter manages model validation
                options.SuppressModelStateInvalidFilter = true;
            });

            services.AddSwaggerGen(ConfigureSwaggerGenOptions);
            services.AddSwaggerGenNewtonsoftSupport();

            services.AddGrpc(ConfigureGrpcServiceOptions);

            services.AddCors(ConfigureCorsOptions);
            
            //directly add Diagnostic Context for UseSerilogRequestLogging
            //from https://github.com/serilog/serilog-aspnetcore/blob/dev/src/Serilog.AspNetCore/SerilogWebHostBuilderExtensions.cs#L156
            //because we are configuring logging in LogConfigurator
            
            var diagnosticContext = new DiagnosticContext(Log.Logger);
            // Consumed by e.g. middleware
            services.AddSingleton(diagnosticContext);
            // Consumed by user code
            services.AddSingleton<IDiagnosticContext>(diagnosticContext);

            services.AddSingleton(Config);

            if (_useJwtAuth)
            {
                services
                    .AddAuthentication(ConfigureAuthenticationOptions)
                    .AddJwtBearer(ConfigureJwtBearerOptions);
            }

            services.AddGrpcReflection();

            ConfigureServicesExt(services);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSerilogRequestLogging(options =>
            {
                options.GetLevel = (httpContext, elapsed, ex) =>
                {
                    if (httpContext.Response.StatusCode >= 500)
                    {
                        return LogEventLevel.Error;
                    }

                    if (httpContext.Response.StatusCode >= 400 || httpContext.Response.StatusCode < 200)
                    {
                        return LogEventLevel.Warning;
                    }

                    return LogEventLevel.Debug;
                };
                
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress);
                    diagnosticContext.Set("DisplayUrl", httpContext.Request.GetDisplayUrl());
                    
                    var logger = httpContext.RequestServices.GetRequiredService<ILogger<SwisschainStartup<TAppSettings>>>();
                    var token = httpContext.ReadJwtSecurityToken(logger);

                    void EnrichWithClaim(string type)
                    {
                        var cl = token?.Claims?.FirstOrDefault(x => x.Type == type);
                        if (cl != null)
                        {
                            diagnosticContext.Set($"Claim-{type}", cl.Value);
                        }
                    }                    
                    
                    void EnrichWithHeader(string headerKey)
                    {
                        if (httpContext.Request.Headers.TryGetValue(headerKey, out var headerValue))
                        {
                            diagnosticContext.Set($"Header-{headerKey}", headerValue);
                        }
                    }
                    
                    EnrichWithClaim(SwisschainClaims.TenantId);
                    EnrichWithClaim(SwisschainClaims.UserId);
                    EnrichWithClaim(SwisschainClaims.ApiKeyId);
                    EnrichWithClaim(SwisschainClaims.UniqueName);
                    EnrichWithHeader("X-Request-ID");
                    EnrichWithHeader("Idempotency-Key");
                    
                    var errorResponse = httpContext.GetErrorResponse();
                    if (errorResponse != null)
                    {
                        diagnosticContext.Set("ErrorResponse", JsonConvert.SerializeObject(errorResponse));
                    }
                    
                    EnrichDiagnosticContext(diagnosticContext, httpContext, token);
                };

            });

            foreach (var (type, args) in ExceptionHandlingMiddlewares)
            {
                app.UseMiddleware(type, args);
            }

            app.UseRouting();

            foreach (var (type, args) in AfterRoutingHandlingMiddlewares)
            {
                app.UseMiddleware(type, args);
            }

            app.UseCors();

            if (_useJwtAuth)
                app.UseAuthentication();

            app.UseAuthorization();

            foreach (var (type, args) in AfterAuthHandlingMiddlewares)
            {
                app.UseMiddleware(type, args);
            }

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                endpoints.MapGrpcReflectionService();

                RegisterEndpoints(endpoints);
            });

            app.UseSwagger(c => c.RouteTemplate = "api/{documentName}/swagger.json");
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("../../api/v1/swagger.json", "API V1");
                c.RoutePrefix = "swagger/ui";
            });

            ConfigureExt(app, env);
        }

        protected virtual void ConfigureSwaggerGen(SwaggerGenOptions swaggerGenOptions)
        {
        }

        protected virtual void ConfigureServicesExt(IServiceCollection services)
        {

        }

        protected virtual void ConfigureExt(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }

        protected virtual void RegisterEndpoints(IEndpointRouteBuilder endpoints)
        {

        }

        protected virtual void ConfigureControllers(MvcOptions options)
        {
        }

        protected virtual void EnrichDiagnosticContext(IDiagnosticContext diagnosticContext, HttpContext httpContext, JwtSecurityToken token)
        {
            
        }

        protected virtual void ConfigureJwtBearerOptions(JwtBearerOptions options)
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSecret)),
                ValidateIssuer = false,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true
            };
        }

        protected virtual void ConfigureMvcNewtonsoftJsonOptions(MvcNewtonsoftJsonOptions options)
        {
            var namingStrategy = new CamelCaseNamingStrategy();

            options.SerializerSettings.Converters.Add(new StringEnumConverter(namingStrategy));
            options.SerializerSettings.NullValueHandling = NullValueHandling.Include;
            options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            options.SerializerSettings.Culture = CultureInfo.InvariantCulture;
            options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            options.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Error;
            options.SerializerSettings.ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = namingStrategy
            };
        }

        protected virtual void ConfigureAuthenticationOptions(AuthenticationOptions options)
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }

        private void ConfigureMvcOptions(MvcOptions options)
        {
            options.Filters.Add(new ProducesAttribute("application/json"));
            options.Filters.Add<ErrorResponseActionFilter>();

            ConfigureControllers(options);
        }

        protected virtual void ConfigureGrpcServiceOptions(GrpcServiceOptions options)
        {

        }

        protected virtual void ConfigureCorsOptions(CorsOptions options)
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyHeader();
                builder.AllowAnyMethod();
                builder.AllowAnyOrigin();
            });
        }

        protected virtual void ConfigureSwaggerGenOptions(SwaggerGenOptions options)
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = ApplicationInformation.AppName, Version = "v1" });
            options.EnableXmsEnumExtension();
            options.MakeResponseValueTypesRequired();

            foreach (var code in ModelStateDictionaryResponseCodes)
            {
                options.AddModelStateDictionaryResponse(code);
            }

            if (_useJwtAuth)
            {
                options.AddJwtBearerAuthorization();
            }

            ConfigureSwaggerGen(options);
        }

    }
}
