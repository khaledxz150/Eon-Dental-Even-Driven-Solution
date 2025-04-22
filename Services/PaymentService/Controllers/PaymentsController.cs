using EventBus.Interfaces;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PaymentService.Data;
using PaymentService.Events;
using PaymentService.Models;

namespace PaymentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly PaymentDbContext _dbContext;
        private readonly IEventBus _eventBus;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            PaymentDbContext dbContext,
            IEventBus eventBus,
            ILogger<PaymentsController> logger)
        {
            _dbContext = dbContext;
            _eventBus = eventBus;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Payment>>> GetAllPayments()
        {
            var payments = await _dbContext.Payments.ToListAsync();
            return Ok(payments);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Payment>> GetPaymentById(Guid id)
        {
            var payment = await _dbContext.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            return Ok(payment);
        }

        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<Payment>> GetPaymentByOrderId(Guid orderId)
        {
            var payment = await _dbContext.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
            if (payment == null)
            {
                return NotFound();
            }

            return Ok(payment);
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment(ProcessPaymentRequest request)
        {
            var payment = await _dbContext.Payments.FirstOrDefaultAsync(p => p.OrderId == request.OrderId);
            if (payment == null)
            {
                return NotFound($"No pending payment found for order {request.OrderId}");
            }

            payment.Status = PaymentStatus.Completed;
            payment.ProcessedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            // Publish payment completed event
            await _eventBus.PublishAsync(new PaymentCompletedEvent
            {
                OrderId = payment.OrderId,
                PaymentId = payment.Id.ToString(),
                PaidAt = payment.ProcessedAt.Value
            });

            _logger.LogInformation("Payment {PaymentId} for order {OrderId} processed successfully",
                payment.Id, payment.OrderId);

            return Ok(payment);
        }
    }
}
