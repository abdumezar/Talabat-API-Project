using System.ComponentModel.DataAnnotations;


namespace Talabat.API.Dtos
{
    public class CustomerBasketDto
    {
        public CustomerBasketDto(string id)
        {
            Id = id;
        }
        [Required]
        public string Id { get; set; }

        public List<BasketItemDto> Items { get; set; } = new List<BasketItemDto>();
        public string? PaymentIntentId { get; set; }
        public string? ClientSecret { get; set; }
        public int? DeliveryMethodId { get; set; }
        public decimal? ShippingCost { get; set; }
    }
}
