using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Talabat.API.Dtos;
using Talabat.API.Errors;
using Talabat.Core.Entities;
using Talabat.Core.IServices;
using Order = Talabat.Core.Entities.Order_Aggregate.Order;

namespace Talabat.API.Controllers
{
    public class PaymentsController : BaseAPIController
    {
        private readonly IPaymentService paymentService;
        private string _whSecret = "whsec_df5bee351a2181daffb67059843e6e5bd0a0ad54aa3154090c5477fc56937a28";
        private readonly IMapper mapper;
        private readonly ILogger<PaymentsController> logger;

        public PaymentsController(IPaymentService paymentService, IMapper mapper, ILogger<PaymentsController> logger)
        {
            this.paymentService = paymentService;
            this.mapper = mapper;
            this.logger = logger;
        }

        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(CustomerBasketDto), StatusCodes.Status200OK)]
        [HttpPost("{basketId}")]
        public async Task<ActionResult<CustomerBasketDto>> CreateOrUpdatePaymentIntent(string basketId)
        {
            var basket = await paymentService.CreateOrUpdatePaymentIntent(basketId);
            
            if (basket is null) return BadRequest(new ApiResponse(400, "Problem with your basket"));
            
            return Ok(basket);
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> StripeWebHook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], _whSecret);

            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;

            Order order;

            // Handle the event
            switch (stripeEvent.Type)
            {
                case Events.PaymentIntentPaymentFailed:
                    order = await paymentService.UpdatePaymentIntentToSucceededOrFailed(paymentIntent.Id, true);
                    logger.LogInformation("Payment Succeeded", paymentIntent.Id);
                    break;
                case Events.PaymentIntentSucceeded:
                    order = await paymentService.UpdatePaymentIntentToSucceededOrFailed(paymentIntent.Id, false);
                    logger.LogInformation("Payment Failed", paymentIntent.Id);
                    break;
                default:

                    Console.WriteLine("Unhandled event type: {0}", stripeEvent.Type);
                    break;
            }

            return Ok();
            
        }
    }
}
