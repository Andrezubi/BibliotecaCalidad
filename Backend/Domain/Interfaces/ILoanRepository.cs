using Backend.Domain.Common;
using Backend.Models;

namespace Backend.Domain.Interfaces;

public interface ILoanRepository
{
    Task<IEnumerable<LoanedBook>> GetLoanedBooksAsync();
    Task<Copy?> GetCopyByIdAsync(uint copyId);

    Task<Loan?> GetActiveLoanByCopyIdAsync(uint copyId);
    Task<Copy?> GetFirstAvailableCopyByBookIdAsync(uint bookId);

    Task SaveChangesAsync();
}