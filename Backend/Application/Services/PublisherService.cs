using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Domain.Interfaces;
using Backend.Models;

namespace Backend.Application.Services;

public class PublisherService : IPublisherService
{
    private readonly IPublisherRepository _publisherRepository;

    public PublisherService(
        IPublisherRepository publisherRepository)
    {
        _publisherRepository = publisherRepository;
    }

    public async Task<IEnumerable<Publisher>> GetAllAsync()
    {
        return await _publisherRepository.GetAllAsync();
    }

    public async Task<Publisher?> GetByIdAsync(uint id)
    {
        var publisher =
            await _publisherRepository.GetByIdAsync(id);

        return publisher;
    }

    public async Task<Publisher> CreateAsync(
        CreatePublisherDto dto)
    {
        var name =
            dto.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "El nombre de la editorial es obligatorio.");
        }

        var exists =
            await _publisherRepository.ExistsAsync(name);

        if (exists)
        {
            throw new InvalidOperationException(
                "Ya existe una editorial activa con ese nombre.");
        }

        var publisher = new Publisher
        {
            Name = name,
            UserId = dto.UserId != null ? (ushort)dto.UserId.Value : null,
            CreatedAt = DateTime.Now
        };

        await _publisherRepository.AddAsync(publisher);

        return publisher;
    }

    public async Task<Publisher?> UpdateAsync(
        uint id,
        CreatePublisherDto dto)
    {
        var publisher =
            await _publisherRepository
                .GetByIdAsync(id);

        if (publisher == null)
        {
            return null;
        }

        var name =
            dto.Name.Trim();

        if (publisher.Name != name)
        {
            var exists =
                await _publisherRepository.ExistsAsync(name);

            if (exists)
            {
                throw new InvalidOperationException(
                    "Ya existe una editorial activa con ese nombre.");
            }
        }

        publisher.Name = name;
        publisher.UserId = dto.UserId != null ? (ushort)dto.UserId.Value : null;
        publisher.UpdatedAt = DateTime.Now;

        await _publisherRepository.UpdateAsync(publisher);

        return publisher;
    }

    public async Task<bool> DeleteAsync(uint id)
    {
        var publisher =
            await _publisherRepository
                .GetByIdAsync(id);

        if (publisher == null)
        {
            return false;
        }

        await _publisherRepository.DeleteAsync(publisher);

        return true;
    }
}