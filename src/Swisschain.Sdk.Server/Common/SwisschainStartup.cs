using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Autofac;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
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

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers(options =>
                {
                    options.Filters.Add(new ProducesAttribute("application/json"));

                    ConfigureControllers(options);
                })
                .AddNewtonsoftJson(options =>
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
                });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = ApplicationInformation.AppName, Version = "v1" });
                c.EnableXmsEnumExtension();
                c.MakeResponseValueTypesRequired();

                foreach (var code in ModelStateDictionaryResponseCodes)
                {
                    c.AddModelStateDictionaryResponse(code);
                }

                if (_useJwtAuth)
                {
                    c.AddJwtBearerAuthorization();
                }

                ConfigureSwaggerGen(c);
            });
            services.AddSwaggerGenNewtonsoftSupport();

            services.AddGrpc();

            services.AddCors(options => options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyHeader();
                builder.AllowAnyMethod();
                builder.AllowAnyOrigin();
            }));

            services.AddSingleton(Config);

            if (_useJwtAuth)
            {
                services
                    .AddAuthentication(x =>
                    {
                        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddJwtBearer(x =>
                    {
                        x.RequireHttpsMetadata = false;
                        x.SaveToken = true;
                        x.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSecret)),
                            ValidateIssuer = false,
                            ValidateAudience = true,
                            ValidAudience = _jwtAudience,
                            ValidateLifetime = true
                        };
                    });
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
    }
}
