using Backend.Application.DTOs;
using Backend.Domain.Common;

namespace Backend.Application.Interfaces
{
    public interface ILoanService
    {
        Task<IEnumerable<LoanedBook>> GetLoanedBooksAsync();
        Task<bool> RegisterReturnAsync(uint copyId);
        Task<bool> RegisterLoanAsync(uint bookId);
        Task<IEnumerable<LoanListDto>> GetAllLoansAsync();
        Task<LoanDto?> GetLoanByIdAsync(uint id);
    }
}