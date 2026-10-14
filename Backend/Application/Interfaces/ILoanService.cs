using Backend.Application.DTOs;
using Backend.Domain.Common;

namespace Backend.Application.Interfaces
{
    public interface ILoanService
    {
        Task<IEnumerable<LoanedBook>> GetLoanedBooksAsync();
        Task<bool> RegisterReturnAsync(uint copyId);
        // Registra un préstamo para múltiples libros (bookIds) para un borrower.
        // Devuelve (Success, ErrorMessage, LoanId)
        Task<(bool Success, string? Error, int? LoanId)> RegisterLoanAsync(uint borrowerId, IEnumerable<uint> bookIds, uint librarianId, int loanDays = 14);
        Task<IEnumerable<LoanListDto>> GetAllLoansAsync();
        Task<LoanDto?> GetLoanByIdAsync(uint id);
    }
}