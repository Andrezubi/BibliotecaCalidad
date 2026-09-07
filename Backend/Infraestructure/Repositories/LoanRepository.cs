using Backend.Domain.Interfaces;
using Backend.Domain.Models;
using Backend.Infraestructure.Persistence;
using Backend.Infrastructure.Persistence;
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

    public async Task<bool> ReturnLoanAsync(int copyId)
    {
        // 1. Buscar la copia física
        var copy = await _context.Copies.FindAsync(copyId);
        if (copy == null)
        {
            return false;
        }

        // 2. Marcar la copia como disponible
        copy.Status = "Available";

        // 3. Si tiene un préstamo activo asociado en la tabla Loan, cerrarlo
        var loan = await _context.Loans
            .Where(l => l.CopyId == copyId && l.Status == "Active")
            .OrderByDescending(l => l.LoanDate)
            .FirstOrDefaultAsync();

        if (loan != null)
        {
            loan.Status = "Returned";
            loan.ReturnedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }
}