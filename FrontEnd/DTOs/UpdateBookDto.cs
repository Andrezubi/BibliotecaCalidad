namespace FrontEnd.DTOs;

public class UpdateBookDto
{
    public string Title { get; set; } = string.Empty;

    public int? EditionNumber { get; set; }

    public string? ISBN { get; set; }

    public int? PublicationYear { get; set; }

    public string? Publisher { get; set; }

    public int? PageCount { get; set; }

    public string? Description { get; set; }

    public int? UserId { get; set; }

    public IFormFile? CoverImage { get; set; }

    // Autores existentes seleccionados
    public List<int> AuthorIds { get; set; } = new();

    // Categorías existentes seleccionadas
    public List<int> CategoryIds { get; set; } = new();

    // Autores nuevos que se crearán dentro de la misma operación
    public List<CreateAuthorDto> NewAuthors { get; set; } = new();

    // Categorías nuevas que se crearán dentro de la misma operación
    public List<CreateCategoryDto> NewCategories { get; set; } = new();
}