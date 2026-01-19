namespace sobee_API.DTOs.Admin
{
    public sealed class AdminUserResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsLocked { get; set; }
        public bool IsCurrentUser { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
