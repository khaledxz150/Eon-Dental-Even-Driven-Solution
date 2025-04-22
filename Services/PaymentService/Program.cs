
using EventBus.Extensions;
using EventBus.Interfaces;

using Microsoft.EntityFrameworkCore;

using PaymentService.Data;
using PaymentService.EventHandler;
using PaymentService.Events;

namespace PaymentService
{
    public class Program
    {
        public async static Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add database context
            builder.Services.AddDbContext<PaymentDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("PaymentDatabase")));

            // Add RabbitMQ Event Bus
            var rabbitMQConnectionString = builder.Configuration.GetValue<string>("RabbitMQ:ConnectionString");
            var exchangeName = builder.Configuration.GetValue<string>("RabbitMQ:ExchangeName");
            builder.Services.AddRabbitMQEventBus(rabbitMQConnectionString, exchangeName);

            // Register event handlers
            builder.Services.AddTransient<OrderCreatedEventHandler>();

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            var eventBus = app.Services.GetRequiredService<IEventBus>();
            await eventBus.SubscribeAsync<OrderCreatedEvent, OrderCreatedEventHandler>();

            await app.RunAsync();
        }
    }
}
