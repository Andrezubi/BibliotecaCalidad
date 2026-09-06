namespace Backend.Domain.Validators;

public class BookValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; }

    public BookValidationException(
        Dictionary<string, string[]> errors)
        : base("Uno o más errores de validación.")
    {
        Errors = errors;
    }
}