using EventBus.Abstracts;

namespace PaymentService.Events
{
    public class PaymentCompletedEvent : IntegrationEvent
    {
        public Guid OrderId { get; set; }
        public string PaymentId { get; set; }
        public DateTime PaidAt { get; set; }
    }
}
