using Backend.Domain.Models;

namespace Backend.Domain.Interfaces;

public interface ILoanRepository
{
    Task<IEnumerable<LoanedBook>> GetLoanedBooksAsync();
    Task<Copy?> GetCopyByIdAsync(int copyId);

    Task<Loan?> GetActiveLoanByCopyIdAsync(int copyId);

    Task SaveChangesAsync();
}