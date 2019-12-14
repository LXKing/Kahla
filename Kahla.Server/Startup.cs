﻿using Aiursoft.Pylon;
using Aiursoft.Pylon.Middlewares;
using Kahla.SDK.Models;
using Kahla.Server.Data;
using Kahla.Server.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using WebPush;

namespace Kahla.Server
{
    public class Startup
    {
        private IConfiguration _configuration { get; }

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
            };
            services.AddDbContext<KahlaDbContext>(options =>
                options.UseSqlServer(_configuration.GetConnectionString("DatabaseConnection")));

            services.AddIdentity<KahlaUser, IdentityRole>()
                .AddEntityFrameworkStores<KahlaDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<List<DomainSettings>>(_configuration.GetSection("AppDomain"));

            services.ConfigureApplicationCookie(t => t.Cookie.SameSite = SameSiteMode.None);

            services.AddControllers().AddNewtonsoftJson(opt =>
            {
                opt.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                opt.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });

            services.AddAiurDependencies<KahlaUser>("Kahla");
            services.AddScoped<WebPushClient>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<HandleKahlaOptionsMiddleware>();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseMiddleware<HandleRobotsMiddleware>();
                app.UseMiddleware<UserFriendlyServerExceptionMiddeware>();
                app.UseMiddleware<UserFriendlyNotFoundMiddeware>();
            }

            app.UseAiursoftDefault();
        }
    }
}
