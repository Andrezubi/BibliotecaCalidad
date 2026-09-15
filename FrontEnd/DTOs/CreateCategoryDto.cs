namespace FrontEnd.DTOs;

    public class CreateCategoryDto
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int? UserId { get; set; }
    }
