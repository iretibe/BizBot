namespace BizBot.Admin.Models
{
    public class CustomerDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = "";
        public string FirstName { get; init; } = "";
        public string LastName { get; init; } = "";
        public string Plan { get; init; } = "";
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
