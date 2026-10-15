using Backend.Application.DTOs;
using Backend.Domain.Common;
using Backend.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace Backend.Domain.Interfaces;

public interface ILoanRepository
{
    Task<IEnumerable<LoanedBook>> GetLoanedBooksAsync();
    Task<Copy?> GetCopyByIdAsync(uint copyId);

    Task<Loan?> GetActiveLoanByCopyIdAsync(uint copyId);
    Task<Copy?> GetFirstAvailableCopyByBookIdAsync(uint bookId);

    // Devuelve la primera copia disponible pero dentro de una transacción con FOR UPDATE
    Task<Copy?> GetFirstAvailableCopyByBookIdForUpdateAsync(uint bookId);

    Task<IDbContextTransaction> BeginTransactionAsync();

    Task AddLoanAsync(Loan loan);

    Task AddFineAsync(Fine fine);

    Task<Fine?> GetFineByLoanCopyAsync(uint loanId, uint copyId);

    Task SaveChangesAsync();
    Task<IEnumerable<LoanListDto>> GetAllLoansAsync();
    Task<Loan?> GetLoanWithItemsByIdAsync(uint id);
}
