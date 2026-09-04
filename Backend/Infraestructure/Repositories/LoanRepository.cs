using Backend.Domain.Interfaces;
using Backend.Domain.Models;
using Backend.Infraestructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Repositories;

public class LoanRepository : BaseRepository<Loan>, ILoanRepository
{
    public LoanRepository(LibraryDbContext context)
        : base(context)
    {
    }

    public async Task<IEnumerable<LoanedBook>> GetLoanedBooksAsync()
    {
        return await _context.Copies
            .Where(c =>
                c.Status == "Loaned" &&
                c.IsActive == true &&
                c.Book.IsActive == true
            )
            .Select(c => new LoanedBook
            {
                CopyId = c.Id,
                BookId = c.Book.Id,
                Title = c.Book.Title,
                InternalCode = c.InternalCode,
                Status = c.Status
            })
            .ToListAsync();
    }
}