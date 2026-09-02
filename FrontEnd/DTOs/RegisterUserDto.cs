using System.ComponentModel.DataAnnotations;

namespace FrontEnd.DTOs
{
    public class RegisterUserDto
    {
        [Required(ErrorMessage = "El CI es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El CI no es válido.")]
        public int CI { get; set; }

        [StringLength(2, ErrorMessage = "El complemento no puede superar los 2 caracteres.")]
        public string? Complement { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public string Password { get; set; } = string.Empty;
    }
}