using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using mycms.Data;
using mycms_shared.Infrastructure;
using mycms.Models.ApplicationServices;
using RabbitMQ.Client;

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
            
            var factory = new ConnectionFactory()
                {
                    HostName = Environment.GetEnvironmentVariable("RABBIT_HOSTNAME"),
                    UserName = Environment.GetEnvironmentVariable("RABBIT_USER"),
                    Password = Environment.GetEnvironmentVariable("RABBIT_PASSWORD"),
                    Port = int.Parse(Environment.GetEnvironmentVariable("RABBIT_PORT"))
                };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            channel.ExchangeDeclare(exchange: "mycms", type: ExchangeType.Fanout);
            
            services.AddSingleton<IModel>(channel);

            services.AddControllersWithViews();
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
