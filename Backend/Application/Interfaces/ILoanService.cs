using Backend.Application.DTOs;
using Backend.Domain.Common;

namespace Backend.Application.Interfaces
{
    public interface ILoanService
    {
        Task<IEnumerable<LoanedBook>> GetLoanedBooksAsync();
        Task<ReturnResultDto?> RegisterReturnAsync(uint copyId);
        Task<bool> RegisterLoanAsync(uint bookId);
        Task<IEnumerable<LoanListDto>> GetAllLoansAsync();
        Task<LoanDto?> GetLoanByIdAsync(uint id);
        Task<bool> UpdateLoanAsync(uint id, UpdateLoanDto dto);
    }
}