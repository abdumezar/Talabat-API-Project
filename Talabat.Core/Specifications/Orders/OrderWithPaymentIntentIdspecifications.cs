using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Talabat.Core.Entities.Order_Aggregate;

namespace Talabat.Core.Specifications.Orders
{
    public class OrderWithPaymentIntentIdspecifications : BaseSpecification<Order>
    {
        public OrderWithPaymentIntentIdspecifications(string paymenyId)
            :base(o=> paymenyId == o.PaymentIntentId)
        {
            
        }
    }
}
