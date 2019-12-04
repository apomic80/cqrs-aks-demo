using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using mycms.Data;
using mycms.Data.Infrastructure;
using mycms.Models.ApplicationServices;
using StackExchange.Redis;

namespace mycms
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_CONNECTIONSTRING");
            var password = Environment.GetEnvironmentVariable("SA_PASSWORD");
            connectionString = connectionString.Replace("{password}", password);

            services.AddDbContext<MyCmsDbContext>(
                opt => opt.UseSqlServer(connectionString));

            services.AddTransient(typeof(IRepository<,>), typeof(EFRepository<,>));
            services.AddTransient<IArticlesApplicationService, ArticlesApplicationService>();

            services.AddControllersWithViews();

            var redisConnString = Environment.GetEnvironmentVariable("REDIS_CONNECTIONSTRING");
            var redisPassword = Environment.GetEnvironmentVariable("REDIS_PASSWORD");
            redisConnString = redisConnString.Replace("{password}", redisPassword);
            
            IDatabase database = ConnectionMultiplexer.Connect(redisConnString).GetDatabase();
            services.AddSingleton<IDatabase>(database);

            /*services.AddDistributedRedisCache(cfg => 
            {
                cfg.Configuration = Configuration.GetConnectionString("RedisConnection");
            });*/
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Articles}/{action=Index}/{id?}");
            });
        }
    }
}
