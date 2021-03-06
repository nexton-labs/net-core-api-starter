﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Seed.Api.Authorization;
using Seed.Api.Filters;
using Seed.Api.Middleware;
using Seed.Data.EF;
using Seed.Domain.Services;
using Seed.Domain.Services.Interfaces;
using Seed.Infrastructure.AuthZero;
using Seed.Infrastructure.RestClient;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;

namespace Seed.Api
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<WebApiCoreSeedContext>(options => options.UseInMemoryDatabase(Environment.MachineName));

            // Add framework services.
            services.AddMvc();

            IAuthorizationPolicies authorizationPolicies = new AuthorizationPolicies();
            services.AddSingleton(authorizationPolicies);

            services.AddAuthorization(options =>
            {
                options.AddPolicy(nameof(AuthorizationPolicies.AdminOnly), authorizationPolicies.AdminOnly);
            });

            //Registers the use of a jwt token
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(option =>
            {
                option.Audience = Configuration["auth0:clientId"];
                option.Authority = $"https://{Configuration["auth0:domain"]}/";
            });

            // Creates the Swagger json based on the documented xml/attributes of the endpoints.
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", GetSwaggerMetadata());
                var xmlFile = $"{Assembly.GetEntryAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
                c.OperationFilter<ValidateModelResponseOperationFilter>();
            });

            // Register Infrastructure dependencies
            services.AddScoped<IRestClient>(sp => new RestClient($"https://{Configuration["auth0:domain"]}", new HttpClient()));
            services.AddSingleton<IAuthZeroClient>(sp => new AuthZeroClient(sp.GetRequiredService<IRestClient>(), Configuration["auth0:NonInteractiveClientId"], Configuration["auth0:NonInteractiveClientSecret"], Configuration["auth0:domain"]));
            services.AddTransient<IAuthZeroService>(sp => new AuthZeroService(sp.GetRequiredService<IAuthZeroClient>()));

            // Register Services
            services.AddTransient<IUserService>(sp => new UserService(sp.GetRequiredService<WebApiCoreSeedContext>()));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, WebApiCoreSeedContext dbContext)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseMiddleware(typeof(ErrorHandlingMiddleware));
            app.UseMiddleware(typeof(AuthorizationMiddleware));
            app.UseSwagger();

            // Enable middleware to serve Swagger UI (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                // Sets Swagger UI route on root, "GET {baseUrl}/".
                c.RoutePrefix = string.Empty;
                c.SwaggerEndpoint("/swagger/v1/swagger.json", ".NET Core API Seed");
            });

            app.UseMvc();

            DatabaseSeed.Initialize(dbContext);
        }

        private static Info GetSwaggerMetadata()
        {
            return new Info
            {
                Title = ".NET Core API Seed",
                Version = "v1",
                Description = "Seed for an API using .NET Core",
                TermsOfService = "https://github.com/NextonLabs/WebApiCore-Seed",
                Contact = new Contact
                {
                    Name = "Nextonlabs",
                    Email = "info@nextonlabs.com"
                },
                License = new License
                {
                    Name = "MIT",
                    Url = "https://github.com/NextonLabs/WebApiCore-Seed/blob/master/LICENSE"
                }
            };
        }
    }
}
