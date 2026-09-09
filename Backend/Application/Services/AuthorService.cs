using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Domain.Interfaces;
using Backend.Domain.Models;

namespace Backend.Application.Services
{
    public class AuthorService : IAuthorService
    {
        private readonly IAuthorRepository _authorRepository;

        public AuthorService(IAuthorRepository authorRepository)
        {
            _authorRepository = authorRepository;
        }

        public async Task<IEnumerable<Author>> GetAllAsync()
        {
            var authors = await _authorRepository.GetAllAsync();

            return authors;
        }

        public async Task<Author?> GetByIdAsync(int id)
        {
            var author = await _authorRepository.GetByIdAsync(id);

            if (author == null || !author.IsActive)
                return null;

            return author;
        }

        public async Task<Author> CreateAsync(
            CreateAuthorDto dto)
        {
            var exists = await _authorRepository.ExistsAsync(
                dto.FirstName,
                dto.LastName);

            if (exists)
            {
                throw new InvalidOperationException(
                    "Ya existe un autor activo con ese nombre.");
            }

            var author = new Author
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
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
            var author = await _authorRepository.GetByIdAsync(id);

            if (author == null || !author.IsActive)
                return null;

            if (author.FirstName != dto.FirstName ||
                author.LastName != dto.LastName)
            {
                var exists = await _authorRepository.ExistsAsync(
                    dto.FirstName,
                    dto.LastName);

                if (exists)
                {
                    throw new InvalidOperationException(
                        "Ya existe un autor activo con ese nombre.");
                }
            }

            author.FirstName = dto.FirstName;
            author.LastName = dto.LastName;
            author.UserId = dto.UserId;
            author.UpdatedAt = DateTime.Now;

            await _authorRepository.UpdateAsync(author);

            return author;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var author = await _authorRepository.GetByIdAsync(id);

            if (author == null || !author.IsActive)
                return false;

            await _authorRepository.DeleteAsync(author);

            return true;
        }
    }
}
