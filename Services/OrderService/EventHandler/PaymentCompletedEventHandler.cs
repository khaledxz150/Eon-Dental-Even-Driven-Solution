using Enums;

using EventBus.Handlers;

using Microsoft.EntityFrameworkCore;

using OrderService.Data;
using OrderService.Events;

namespace OrderService.EventHandler
{
    public class PaymentCompletedEventHandler : IIntegrationEventHandler<PaymentCompletedEvent>
    {
        private readonly OrderDbContext _dbContext;
        private readonly ILogger<PaymentCompletedEventHandler> _logger;

        public PaymentCompletedEventHandler(
            OrderDbContext dbContext,
            ILogger<PaymentCompletedEventHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task HandleAsync(PaymentCompletedEvent @event)
        {
            _logger.LogInformation("Handling PaymentCompleted event for order {OrderId}", @event.OrderId);

            var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == @event.OrderId);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found when processing PaymentCompleted event", @event.OrderId);
                return;
            }

            order.Status = OrderStatus.PaymentCompleted;
            order.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Order {OrderId} updated to status {Status}", order.Id, order.Status);
        }
    }
}
