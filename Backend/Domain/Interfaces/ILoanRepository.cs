using Backend.Domain.Models;

namespace Backend.Domain.Interfaces;

public interface ILoanRepository
{
    Task<IEnumerable<LoanedBook>> GetLoanedBooksAsync();
    Task<bool> ReturnLoanAsync(int copyId);
}