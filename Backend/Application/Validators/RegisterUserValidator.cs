using Backend.Application.DTOs;

namespace Backend.Application.Validators
{
    public class RegisterUserValidator
    {
        public List<string> Validate(RegisterUserRequest request)
        {
            var errors = new List<string>();

            if (request.CI <= 0)
            {
                errors.Add("El CI es obligatorio.");
            }

            if (!string.IsNullOrWhiteSpace(request.Complement)
                && request.Complement.Length > 2)
            {
                errors.Add("El complemento no puede tener más de 2 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(request.FirstName))
            {
                errors.Add("El nombre es obligatorio.");
            }

            if (request.FirstName.Length > 100)
            {
                errors.Add("El nombre no puede superar los 100 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(request.LastName))
            {
                errors.Add("El apellido es obligatorio.");
            }

            if (request.LastName.Length > 100)
            {
                errors.Add("El apellido no puede superar los 100 caracteres.");
            }

            if (!string.IsNullOrWhiteSpace(request.Phone)
                && request.Phone.Length > 30)
            {
                errors.Add("El teléfono no puede superar los 30 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(request.Username))
            {
                errors.Add("El nombre de usuario es obligatorio.");
            }

            if (request.Username.Length > 50)
            {
                errors.Add("El nombre de usuario no puede superar los 50 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                errors.Add("La contraseña es obligatoria.");
            }
            else if (request.Password.Length < 6)
            {
                errors.Add("La contraseña debe tener al menos 6 caracteres.");
            }

            return errors;
        }
    }
}