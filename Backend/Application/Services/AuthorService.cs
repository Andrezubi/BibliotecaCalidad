
using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Domain.Interfaces;
using Backend.Domain.Validators;
using Backend.Models;

namespace Backend.Application.Services;

public class AuthorService : IAuthorService
{
    private readonly IAuthorRepository _authorRepository;

    public AuthorService(IAuthorRepository authorRepository)
    {
        _authorRepository = authorRepository;
    }

    public async Task<IEnumerable<Author>> GetAllAsync()
    {
        return await _authorRepository.GetAllAsync();
    }

    public async Task<Author?> GetByIdAsync(uint id)
    {
        var author = await _authorRepository.GetByIdAsync(id);

        if (author == null || author.IsActive != true)
        {
            return null;
        }

        return author;
    }

    public async Task<Author> CreateAsync(CreateAuthorDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        // Normalizar los datos recibidos.
        var firstName = dto.FirstName?.Trim() ?? string.Empty;
        var lastName = dto.LastName?.Trim() ?? string.Empty;

        // Validar nombre y apellido antes de guardar.
        AuthorValidator.Validate(new Author
        {
            FirstName = firstName,
            LastName = lastName
        });

        // Verificar que no exista un autor activo duplicado.
        var exists = await _authorRepository.ExistsAsync(
            firstName,
            lastName
        );

        if (exists)
        {
            throw new InvalidOperationException(
                "Ya existe un autor activo con ese nombre.");
        }

        // Crear el autor.
        var author = new Author
        {
            FirstName = firstName,
            LastName = lastName,
            UserId = dto.UserId != null
                ? (ushort)dto.UserId.Value
                : null,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        await _authorRepository.AddAsync(author);

        return author;
    }

    public async Task<Author?> UpdateAsync(
        uint id,
        CreateAuthorDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var author = await _authorRepository.GetByIdAsync(id);

        if (author == null || author.IsActive != true)
        {
            return null;
        }

        var firstName = dto.FirstName?.Trim() ?? string.Empty;
        var lastName = dto.LastName?.Trim() ?? string.Empty;

        // Aplicar las mismas validaciones al actualizar.
        AuthorValidator.Validate(new Author
        {
            FirstName = firstName,
            LastName = lastName,
            SecondLastName = author.SecondLastName
        });

        // Comprobar duplicados si cambió el nombre o apellido.
        if (author.FirstName != firstName ||
            author.LastName != lastName)
        {
            var exists = await _authorRepository.ExistsAsync(
                firstName,
                lastName
            );

            if (exists)
            {
                throw new InvalidOperationException(
                    "Ya existe un autor activo con ese nombre.");
            }
        }

        // Actualizar los datos.
        author.FirstName = firstName;
        author.LastName = lastName;
        author.UserId = dto.UserId != null
            ? (ushort)dto.UserId.Value
            : null;
        author.UpdatedAt = DateTime.Now;

        await _authorRepository.UpdateAsync(author);

        return author;
    }

    public async Task<bool> DeleteAsync(uint id)
    {
        var author = await _authorRepository.GetByIdAsync(id);

        if (author == null || author.IsActive != true)
        {
            return false;
        }

        await _authorRepository.DeleteAsync(author);

        return true;
    }
}
