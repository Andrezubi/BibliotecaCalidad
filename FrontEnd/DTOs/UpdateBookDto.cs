using System.ComponentModel.DataAnnotations;

namespace FrontEnd.DTOs;

public class UpdateBookDto
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(200, ErrorMessage = "El título no puede superar los 200 caracteres.")]
    public string Title { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "La edición debe ser mayor a 0.")]
    public int EditionNumber { get; set; }

    [StringLength(20, ErrorMessage = "El ISBN no puede superar los 20 caracteres.")]
    public string? ISBN { get; set; }

    [Range(1000, 2100, ErrorMessage = "Ingrese un año válido.")]
    public int? PublicationYear { get; set; }

    [StringLength(150, ErrorMessage = "La editorial no puede superar los 150 caracteres.")]
    public string? Publisher { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad de páginas debe ser mayor a 0.")]
    public int? PageCount { get; set; }

    public string? Description { get; set; }

    public int UserId { get; set; }
}