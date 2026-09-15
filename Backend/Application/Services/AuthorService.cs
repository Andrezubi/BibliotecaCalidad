using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Domain.Interfaces;
using Backend.Domain.Models;

namespace Backend.Application.Services;

public class AuthorService : IAuthorService
{
    private readonly IAuthorRepository _authorRepository;

    public AuthorService(
        IAuthorRepository authorRepository)
    {
        _authorRepository = authorRepository;
    }

    public async Task<IEnumerable<Author>> GetAllAsync()
    {
        return await _authorRepository.GetAllAsync();
    }

    public async Task<Author?> GetByIdAsync(int id)
    {
        var author =
            await _authorRepository.GetByIdAsync(id);

        if (author == null ||
            !author.IsActive)
        {
            return null;
        }

        return author;
    }

    public async Task<Author> CreateAsync(
        CreateAuthorDto dto)
    {
        var firstName =
            dto.FirstName.Trim();

        var lastName =
            dto.LastName.Trim();

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException(
                "El nombre del autor es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException(
                "El apellido del autor es obligatorio.");
        }

        var exists =
            await _authorRepository.ExistsAsync(
                firstName,
                lastName);

        if (exists)
        {
            throw new InvalidOperationException(
                "Ya existe un autor activo con ese nombre.");
        }

        var author = new Author
        {
            FirstName = firstName,
            LastName = lastName,
            UserId = dto.UserId,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        await _authorRepository.AddAsync(author);

        return author;
    }

    public async Task<Author?> UpdateAsync(
        int id,
        CreateAuthorDto dto)
    {
        var author =
            await _authorRepository
                .GetByIdAsync(id);

        if (author == null ||
            !author.IsActive)
        {
            return null;
        }

        var firstName =
            dto.FirstName.Trim();

        var lastName =
            dto.LastName.Trim();

        if (author.FirstName != firstName ||
            author.LastName != lastName)
        {
            var exists =
                await _authorRepository.ExistsAsync(
                    firstName,
                    lastName);

            if (exists)
            {
                throw new InvalidOperationException(
                    "Ya existe un autor activo con ese nombre.");
            }
        }

        author.FirstName = firstName;
        author.LastName = lastName;
        author.UserId = dto.UserId;
        author.UpdatedAt = DateTime.Now;

        await _authorRepository.UpdateAsync(author);

        return author;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var author =
            await _authorRepository
                .GetByIdAsync(id);

        if (author == null ||
            !author.IsActive)
        {
            return false;
        }

        await _authorRepository.DeleteAsync(author);

        return true;
    }
}