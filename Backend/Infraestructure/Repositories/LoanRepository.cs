using Backend.Application.DTOs;
using Backend.Domain.Common;
using Backend.Domain.Interfaces;
using Backend.Infraestructure.Persistence;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Backend.Infraestructure.Repositories;

public class LoanRepository : ILoanRepository
{
    private readonly LibraryDbcontext _context;

    public LoanRepository(LibraryDbcontext context)
    {
        _context = context;
    }

    public async Task<Copy?> GetFirstAvailableCopyByBookIdForUpdateAsync(uint bookId)
    {
        // Usamos SELECT ... FOR UPDATE para evitar race conditions cuando se ejecuta dentro de una transacción.
        // Aseguramos el nombre de la tabla con mayúscula tal como está definida en la base de datos.
        var sql = "SELECT * FROM `Copy` WHERE `BookId` = {0} AND `StatusId` = 1 AND `IsActive` = 1 ORDER BY `Id` LIMIT 1 FOR UPDATE";
        return await _context.Copies.FromSqlRaw(sql, bookId).FirstOrDefaultAsync();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _context.Database.BeginTransactionAsync();
    }

    public async Task AddLoanAsync(Loan loan)
    {
        await _context.Loans.AddAsync(loan);
        await _context.SaveChangesAsync();
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
        .Include(l => l.Borrower)
        .Include(l => l.Loanitems)
            .ThenInclude(li => li.Copy)
                .ThenInclude(c => c.Book)
        .Where(l => l.Loanitems.Any(li => li.CopyId == copyId && (li.Status == "Active" || li.Status == "Overdue")))
        .OrderByDescending(l => l.LoanDate)
        .FirstOrDefaultAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task AddFineAsync(Fine fine)
    {
        await _context.Fines.AddAsync(fine);
        await _context.SaveChangesAsync();
    }

    public async Task<Fine?> GetFineByLoanCopyAsync(uint loanId, uint copyId)
    {
        return await _context.Fines
            .FirstOrDefaultAsync(f => f.LoanId == loanId && f.CopyId == copyId);
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

    public async Task<IEnumerable<LoanListDto>> GetAllLoansAsync()
    {
        return await _context.Loans
            .OrderByDescending(l => l.LoanDate)
            .Select(l => new LoanListDto
            {
                Id = (int)l.Id,
                BorrowerName = l.Borrower.FirstName + " " + l.Borrower.LastName,
                LibrarianName = l.Librarian.FirstName + " " + l.Librarian.LastName,
                LoanDate = l.LoanDate,
                DueDate = l.DueDate,
                Status = l.Status,
                StatusDisplayName = GetLoanStatusDisplayName(l.Status),
                ItemsCount = l.Loanitems.Count,
                ReturnedItemsCount = l.Loanitems.Count(li => li.Status == "Returned")
            })
            .ToListAsync();
    }

    public async Task<Loan?> GetLoanWithItemsByIdAsync(uint id)
    {
        return await _context.Loans
            .Include(l => l.Borrower)
            .Include(l => l.Librarian)
            .Include(l => l.Loanitems)
                .ThenInclude(li => li.Copy)
                    .ThenInclude(c => c.Book)
            .Include(l => l.Loanitems)
                .ThenInclude(li => li.Copy)
                    .ThenInclude(c => c.Status)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    private static string GetLoanStatusDisplayName(string status)
    {
        return status switch
        {
            "Active" => "Activo",
            "Completed" => "Completado",
            "Overdue" => "Vencido",
            "Lost" => "Perdido",
            _ => status
        };
    }

    public async Task<IEnumerable<Loan>> GetLoansForStatusUpdateAsync()
    {
        return await _context.Loans
            .Where(l => l.Status != "Completed" && l.Status != "Lost")
            .Include(l => l.Loanitems)
            .ToListAsync();
    }
}