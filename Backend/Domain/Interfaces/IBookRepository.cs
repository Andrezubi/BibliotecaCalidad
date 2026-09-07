using Backend.Domain.Models;
using Backend.Infrastructure.Persistence;

namespace Backend.Domain.Interfaces;

public interface IBookRepository : IBaseRepository<Book>
{
}