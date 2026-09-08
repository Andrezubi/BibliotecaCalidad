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
    public async Task<Copy?> GetCopyByIdAsync(int copyId)
    {
        return await _context.Copies
            .FirstOrDefaultAsync(c => c.Id == copyId);
    }

    public async Task<Loan?> GetActiveLoanByCopyIdAsync(int copyId)
    {
        return await _context.Loans
            .Where(l => l.CopyId == copyId && l.Status == "Active")
            .OrderByDescending(l => l.LoanDate)
            .FirstOrDefaultAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
    public async Task<Copy?> GetFirstAvailableCopyByBookIdAsync(int bookId)
    {
        return await _context.Copies
            .Where(c =>
                c.BookId == bookId &&
                c.Status == "Available" &&
                c.IsActive == true)
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync();
    }
}