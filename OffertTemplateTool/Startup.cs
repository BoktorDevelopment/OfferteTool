﻿using System;
using System.Linq;
using System.Threading.Tasks;
using OffertTemplateTool.Connectors;
using OffertTemplateTool.DAL.Models;
using OffertTemplateTool.DAL.Context;
using OffertTemplateTool.TemplateService;
using OffertTemplateTool.DAL.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DinkToPdf;
using DinkToPdf.Contracts;

namespace OffertTemplateTool
{

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddAzureAd(options => Configuration.Bind("AzureAd", options))
            .AddCookie();

            DataBaseContext.ConnectionString = Configuration.GetConnectionString("DataBaseContext");
            services.AddDbContext<DataBaseContext>();

            services.AddScoped<IRepository<Offers>, OfferRepository>();
            services.AddScoped<IRepository<Users>, UsersRepository>();
            services.AddScoped<IRepository<Settings>, SettingsRepository>();
            services.AddScoped<IRepository<Estimates>, EstimateRepository>();
            services.AddScoped<IRepository<EstimateLines>, EstimateLinesRepository>();
            services.AddScoped<IRepository<EstimateConnects>, EstimateConnectsRepository>();
            services.AddScoped<IConnector, WeFactConnector>();
            services.AddScoped<ITemplateService, TemplateServiceClass>();

            services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider services)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
            SeedDatabase(services).Wait();
        }

        public async Task SeedDatabase(IServiceProvider services)
        {
            using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var repositoryx = scope.ServiceProvider.GetRequiredService<IRepository<Settings>>();
                // hier je zaad code
                if (!repositoryx.GetAll().Any())
                {
                    var newReg = new Settings()
                    {
                        Key = "DocumentCode",
                        Value = "0"
                    };
                    repositoryx.Add(newReg);
                    await repositoryx.SaveChangesAsync();
                };
            }
        }
    }
}
