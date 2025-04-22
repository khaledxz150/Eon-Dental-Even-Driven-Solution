using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventBus.Bus;
using EventBus.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventBus.Extensions
{

    public static class EventBusExtensions
    {
        public  static IServiceCollection AddRabbitMQEventBus(this IServiceCollection services, string connectionString, string exchangeName)
        {
            services.AddSingleton<IEventBus>(sp =>
            {
                var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                var logger = sp.GetRequiredService<ILogger<RabbitMQEventBus>>();
                return RabbitMQEventBus.CreateAsync(scopeFactory, logger, connectionString, exchangeName).GetAwaiter().GetResult();
            });

            return services;
        }
    }
}
