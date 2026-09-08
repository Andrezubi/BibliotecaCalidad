using Backend.Domain.Models;

namespace Backend.Application.Interfaces
{
    public interface ILoanService
    {
        Task<IEnumerable<LoanedBook>> GetLoanedBooksAsync();
        Task<bool> RegisterReturnAsync(int copyId);
        Task<bool> RegisterLoanAsync(int bookId);
    }
}