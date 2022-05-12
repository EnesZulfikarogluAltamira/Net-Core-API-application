using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPI.Extensions
{
    public static class ConsulRegistration
    {
        public static IServiceCollection ConfigureConsul(this IServiceCollection services, IConfiguration configuration)
        {
            var SecretID = "1bf93d81-6448-0e63-6354-31c728daddc0";
            
            services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
            {
                var address = configuration["consulConfig:Address"];
                consulConfig.Address = new Uri(address);
                consulConfig.Token = SecretID;
            }));

            return services;
        }


        public static IApplicationBuilder RegisterWithConsul(this IApplicationBuilder app, IHostApplicationLifetime lifetime)
        {
            var consulClient = app.ApplicationServices.GetRequiredService<IConsulClient>();

            var loggingFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();

            var logger = loggingFactory.CreateLogger<IApplicationBuilder>();

            //var features = app.Properties["server.Features"] as FeatureCollection;
            //var addresses = features.Get<IServerAddressesFeature>();
            //var address = addresses.Addresses.First();

            var address = "http://localhost:9000";

            var uri = new Uri(address);

            var registration = new AgentServiceRegistration()
            {
                ID = $"EmployeeService",
                Name = "EmployeeService",
                Address = $"{uri.Host}",
                Port = uri.Port,
                Tags = new[] {"Employee Service", "Employee"},
                Check = new AgentServiceCheck()
                {
                    Name = "HealthCheck-1",
                    HTTP = "http://31.210.93.228:1038/health",
                    Method = "GET",
                    Interval = TimeSpan.FromSeconds(10),
                }
            };

            logger.LogInformation("Registering with Consul");

            consulClient.Agent.ServiceDeregister(registration.ID).Wait();
            consulClient.Agent.ServiceRegister(registration).Wait();

            consulClient.Health.Checks("EmployeeService").Wait();

            //lifetime.ApplicationStopping.Register(() =>
            //{
            //    logger.LogInformation("Deregistering from Consul");
            //    consulClient.Agent.ServiceDeregister(registration.ID).Wait();
            //});

            return app;
        }
    }
}