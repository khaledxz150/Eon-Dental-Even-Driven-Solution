using EventBus.Handlers;
using PaymentService.Data;
using PaymentService.Events;
using PaymentService.Models;

namespace PaymentService.EventHandler
{
    public class OrderCreatedEventHandler : IIntegrationEventHandler<OrderCreatedEvent>
    {
        private readonly PaymentDbContext _dbContext;
        private readonly ILogger<OrderCreatedEventHandler> _logger;

        public OrderCreatedEventHandler(
            PaymentDbContext dbContext,
            ILogger<OrderCreatedEventHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task HandleAsync(OrderCreatedEvent @event)
        {
            _logger.LogInformation("Handling OrderCreated event for order {OrderId}", @event.OrderId);

            // Create a pending payment for the order
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = @event.OrderId,
                CustomerId = @event.CustomerId,
                Amount = @event.TotalAmount,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Payments.Add(payment);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Created pending payment {PaymentId} for order {OrderId}",
                payment.Id, payment.OrderId);
        }
    }
}
