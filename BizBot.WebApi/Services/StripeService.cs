//using Stripe;
//using Stripe.Checkout;

//namespace BizBot.WebApi.Services
//{
//    public class StripeService
//    {
//        private readonly IConfiguration _configuration;

//        public string WebhookSecret => _configuration["Stripe:WebhookSecret"]!;

//        public StripeService(IConfiguration configuration)
//        {
//            _configuration = configuration;

//            StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
//        }

//        public async Task<Session> CreateCheckoutSessionAsync(string priceId, 
//            string customerEmail, string successUrl, string cancelUrl)
//        {
//            var options = new SessionCreateOptions
//            {
//                PaymentMethodTypes = new List<string> { "card" },
//                LineItems = new List<SessionLineItemOptions>
//                {
//                    new SessionLineItemOptions
//                    {
//                        Price = priceId,
//                        Quantity = 1
//                    }
//                },
//                Mode = "subscription",
//                SuccessUrl = successUrl,
//                CancelUrl = cancelUrl,
//                CustomerEmail = customerEmail,
//                ClientReferenceId = Guid.NewGuid().ToString(),
//                Metadata = new Dictionary<string, string>
//                {
//                    { "plan", GetPlanName(priceId) }
//                }
//            };

//            var service = new SessionService();
//            return await service.CreateAsync(options);
//        }

//        private string GetPlanName(string priceId)
//        {
//            // Map price IDs to plan names
//            return priceId switch
//            {
//                "price_starter" => "starter",
//                "price_pro" => "pro",
//                "price_business" => "business",
//                _ => "custom"
//            };
//        }
//    }
//}
