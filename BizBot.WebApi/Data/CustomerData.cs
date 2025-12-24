namespace BizBot.WebApi.Data
{
    public class CustomerData
    {
        public Guid Id { get; set; }

        // Identity
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        // Business
        public string CompanyName { get; set; } = string.Empty;
        public string PlanCode { get; set; } = string.Empty;

        // Status
        public bool IsActive { get; set; } = true;
        public CustomerStatus Status { get; set; } = CustomerStatus.Active;

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ActivatedAt { get; set; }
        public DateTime? SuspendedAt { get; set; }

        // External references (payments, CRM, etc.)
        public string? ExternalReference { get; set; }
    }

    public enum CustomerStatus
    {
        Pending,
        Active,
        Suspended,
        Disabled
    }
}
