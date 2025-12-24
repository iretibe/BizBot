using BizBot.WebApi.Responses;
using System.Text;
using System.Text.Json;

namespace BizBot.WebApi.Services
{
    public class PaystackService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, string> _planCodes;

        public string WebhookSecret => _configuration["Paystack:WebhookSecret"]!;

        public PaystackService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();

            // Setup HttpClient for PayStack API
            _httpClient.BaseAddress = new Uri("https://api.paystack.co/");
            _httpClient.DefaultRequestHeaders.Add("Authorization",
                $"Bearer {configuration["Paystack:SecretKey"]}");

            // Load plan codes
            _planCodes = new Dictionary<string, string>
            {
                ["starter"] = configuration["PaystackPlans:Starter"]!,
                ["pro"] = configuration["PaystackPlans:Pro"]!,
                ["agency"] = configuration["PaystackPlans:Agency"]!
            };
        }

        // Create a subscription checkout (initialize transaction)
        public async Task<PaystackTransactionResponse> CreateSubscriptionAsync(
            string planName,
            string customerEmail,
            decimal amount,
            string reference,
            Dictionary<string, string> metadata)
        {
            if (!_planCodes.TryGetValue(planName, out var planCode))
                throw new ArgumentException($"Invalid plan: {planName}");

            var request = new
            {
                email = customerEmail,
                amount = amount * 100, // PayStack uses kobo (Naira * 100)
                plan = planCode,
                reference = reference,
                callback_url = _configuration["Paystack:CallbackUrl"],
                metadata = metadata
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("transaction/initialize", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PaystackTransactionResponse>(responseJson);

            return result!;
        }

        // Verify transaction
        public async Task<PaystackVerifyResponse> VerifyTransactionAsync(string reference)
        {
            var response = await _httpClient.GetAsync($"transaction/verify/{reference}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PaystackVerifyResponse>(json)!;
        }

        // Create a subscription plan (one-time setup)
        public async Task<PaystackPlanResponse> CreatePlanAsync(
            string name,
            decimal amount,
            string interval = "monthly",
            string currency = "GHC")
        {
            var request = new
            {
                name = name,
                amount = amount * 100,
                interval = interval,
                currency = currency
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("plan", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PaystackPlanResponse>(responseJson)!;
        }

        // Create customer
        public async Task<PaystackCustomerResponse> CreateCustomerAsync(
            string email, string firstName, string lastName, string phone = null)
        {
            var request = new
            {
                email = email,
                first_name = firstName,
                last_name = lastName,
                phone = phone
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("customer", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PaystackCustomerResponse>(responseJson)!;
        }

        // Validate webhook signature
        public bool ValidateWebhookSignature(string payload, string signature)
        {
            // PayStack webhook validation
            // In production, compute HMAC SHA512
            var computedSignature = ComputeHmacSha512(payload, WebhookSecret);
            return signature == computedSignature;
        }

        private string ComputeHmacSha512(string payload, string secret)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA512(
                Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}