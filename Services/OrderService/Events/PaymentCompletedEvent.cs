using EventBus.Abstracts;

namespace OrderService.Events
{
    public class PaymentCompletedEvent : IntegrationEvent
    {
        public Guid OrderId { get; set; }
        public string PaymentId { get; set; }
        public DateTime PaidAt { get; set; }
    }
}
