namespace FrontEnd.DTOs;

public class BookDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;

    public bool IsAvailable { get; set; }
}