namespace BizBot.WebApi.Services
{
    public interface IEmailService
    {
        Task SendAsync(string email, string subject, 
            string plan, string billingCycle);
    }
}
