using Microsoft.Extensions.Configuration;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Talabat.Core;
using Talabat.Core.Entities;
using Talabat.Core.Entities.Order_Aggregate;
using Talabat.Core.IRepositories;
using Talabat.Core.IServices;
using Talabat.Core.Specifications.Orders;
using Order = Talabat.Core.Entities.Order_Aggregate.Order;
using Product = Talabat.Core.Entities.Product;

namespace Talabat.Service
{
    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration configuration;
        private readonly IBasketRepository basketRepo;
        private readonly IUnitOfWork unitOfWork;

        public PaymentService(IConfiguration configuration, IBasketRepository basketRepo, IUnitOfWork unitOfWork)
        {
            this.configuration = configuration;
            this.basketRepo = basketRepo;
            this.unitOfWork = unitOfWork;
        }

        public async Task<CustomerBasket> CreateOrUpdatePaymentIntent(string basketId)
        {
            StripeConfiguration.ApiKey = configuration["StripeSettings:SecretKey"];

            var basket = await basketRepo.GetBasketAsync(basketId);

            if (basket is null) return null;

            var shippingPrice = 0m;
            if (basket.DeliveryMethodId.HasValue)
            {
                var deliveryMethod = await unitOfWork.Repository<DeliveryMethod>().GetByIdAsync(basket.DeliveryMethodId.Value);
                basket.ShippingCost = deliveryMethod.Cost;
                shippingPrice += deliveryMethod.Cost;
            }

            if(basket.Items?.Count > 0)
            {
                foreach (var item in basket.Items)
                {
                    var productItem = await unitOfWork.Repository<Product>().GetByIdAsync(item.Id);
                    if (item.Price != productItem.Price)
                        item.Price = productItem.Price;
                }
            }

            var paymentIntentService = new PaymentIntentService();

            PaymentIntent paymentIntent;

            if (string.IsNullOrEmpty(basket.PaymentIntentId))
            {
                var options = new PaymentIntentCreateOptions()
                {
                    Amount = (long)basket.Items.Sum(item => item.Price * item.Quantity * 100) + (long)shippingPrice * 100,
                    Currency = "usd",
                    PaymentMethodTypes = new List<string>() { "card" }
                };

                paymentIntent = await paymentIntentService.CreateAsync(options);
            
                basket.PaymentIntentId = paymentIntent.Id;
                basket.ClientSecret = paymentIntent.ClientSecret;
            }
            else
            {
                var options = new PaymentIntentUpdateOptions()
                {
                    Amount = (long)basket.Items.Sum(item => item.Price * item.Quantity * 100) + (long)shippingPrice * 100
                };
                await paymentIntentService.UpdateAsync(basket.PaymentIntentId, options);
            }

            await basketRepo.UpdateBasketAsync(basket);

            return basket;
        }

        public async Task<Order> UpdatePaymentIntentToSucceededOrFailed(string paymentIntentId, bool IsSucceed)
        {
            var spec = new OrderWithPaymentIntentIdspecifications(paymentIntentId);

            var order = await unitOfWork.Repository<Order>().GetEntityIdWithSpecAsync(spec);

            if (IsSucceed)
                order.Status = OrderStatus.PaymentReceived;
            else
                order.Status = OrderStatus.PaymentFaild;

            unitOfWork.Repository<Order>().Update(order);

            unitOfWork.Complete();

            return order;
        }
    }
}
