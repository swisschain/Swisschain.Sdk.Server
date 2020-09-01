using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Autofac;
using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swisschain.Sdk.Server.Swagger;
using Swisschain.Sdk.Server.WebApi.ExceptionsHandling;

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
            ExceptionHandlingMiddlewares = new List<(System.Type, object[])>();
            ModelStateDictionaryResponseCodes = new HashSet<int>();

            AddExceptionHandlingMiddleware<UnhandledExceptionsMiddleware>();

            ModelStateDictionaryResponseCodes.Add(StatusCodes.Status400BadRequest);
            ModelStateDictionaryResponseCodes.Add(StatusCodes.Status500InternalServerError);
        }

        public IConfiguration ConfigRoot { get; }

        public TAppSettings Config { get; }

        public List<(System.Type Type, object[] Args)> ExceptionHandlingMiddlewares { get; }

        public List<(System.Type Type, object[] Args)> AfterAuthHandlingMiddlewares { get; }

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


        protected void  AddAfterAuthHandlingMiddlewares<TMiddleware>(params object[] args)
        {
            AfterAuthHandlingMiddlewares.Add((typeof(TMiddleware), args));
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

        public void ConfigureContainer(ContainerBuilder builder)
        {
            ConfigureContainerExt(builder);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            foreach (var (type, args) in ExceptionHandlingMiddlewares)
            {
                app.UseMiddleware(type, args);
            }

            app.UseRouting();

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

        protected virtual void ConfigureContainerExt(ContainerBuilder builder)
        {

        }

        protected virtual void RegisterEndpoints(IEndpointRouteBuilder endpoints)
        {

        }

        protected virtual void ConfigureControllers(MvcOptions options)
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
