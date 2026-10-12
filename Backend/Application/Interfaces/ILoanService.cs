using Backend.Domain.Common;

namespace Backend.Application.Interfaces
{
    public interface ILoanService
    {
        Task<IEnumerable<LoanedBook>> GetLoanedBooksAsync();
        Task<bool> RegisterReturnAsync(uint copyId);
        Task<bool> RegisterLoanAsync(uint bookId);
    }
}