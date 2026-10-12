namespace Backend.Application.DTOs;

public class CreatePublisherDto
{
    public string Name { get; set; } = string.Empty;

    public int? UserId { get; set; }
}