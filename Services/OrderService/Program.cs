
using EventBus.Extensions;
using EventBus.Interfaces;

using Microsoft.EntityFrameworkCore;

using OrderService.Data;
using OrderService.EventHandler;
using OrderService.Events;

namespace OrderService
{
    public class Program
    {
        public async static Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Order Service API",
                    Version = "v1",
                    Description = "API for managing orders in the Order Service",
                });
            });


            // Add database context with retry policy
            var connectionString = builder.Configuration.GetConnectionString("OrderDatabase");

            builder.Services.AddDbContext<OrderDbContext>(options =>
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 10,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                }));

            // Add RabbitMQ Event Bus
            var rabbitMQConnectionString = builder.Configuration.GetValue<string>("RabbitMQ:ConnectionString");
            var exchangeName = builder.Configuration.GetValue<string>("RabbitMQ:ExchangeName");
            builder.Services.AddRabbitMQEventBus(rabbitMQConnectionString, exchangeName);

            // Register event handlers
            builder.Services.AddTransient<PaymentCompletedEventHandler>();

            var app = builder.Build();

            // Configure the HTTP request pipeline
          
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service API v1");
                c.RoutePrefix = string.Empty; // To serve Swagger UI at the root
            });
            

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            var eventBus = app.Services.GetRequiredService<IEventBus>();
            await eventBus.SubscribeAsync<PaymentCompletedEvent, PaymentCompletedEventHandler>();

            await app.RunAsync();
        }
    }
}
