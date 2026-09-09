namespace Backend.Application.DTOs
{
    public class CreateAuthorDto
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public int? UserId { get; set; }
    }
}
