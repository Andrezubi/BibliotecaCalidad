using Backend.Domain.Common;
using Backend.Domain.Interfaces;
using Backend.Infraestructure.Persistence;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infraestructure.Repositories;

public class LoanRepository : ILoanRepository
{
    private readonly LibraryDbcontext _context;

    public LoanRepository(LibraryDbcontext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LoanedBook>> GetLoanedBooksAsync()
    {
        return await _context.Copies
            .Where(c =>
                c.StatusId == 2 && // Loaned
                c.Book.IsActive == true
            )
            .Select(c => new LoanedBook
            {
                CopyId = c.Id,
                BookId = c.Book.Id,
                Title = c.Book.Title,
                InternalCode = c.InternalCode,
                Status = c.Status.Name
            })
            .ToListAsync();
    }

    public async Task<Copy?> GetCopyByIdAsync(uint copyId)
    {
        return await _context.Copies
            .FirstOrDefaultAsync(c => c.Id == copyId);
    }

    public async Task<Loan?> GetActiveLoanByCopyIdAsync(uint copyId)
    {
        return await _context.Loans
            .Where(l => l.Loanitems.Any(li => li.CopyId == copyId && li.Status == "Active"))
            .OrderByDescending(l => l.LoanDate)
            .FirstOrDefaultAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<Copy?> GetFirstAvailableCopyByBookIdAsync(uint bookId)
    {
        return await _context.Copies
            .Where(c =>
                c.BookId == bookId &&
                c.StatusId == 1 && // Available
                c.Book.IsActive == true)
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync();
    }
}