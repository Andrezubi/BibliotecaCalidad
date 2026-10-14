using Backend.Application.DTOs;
using Backend.Domain.Common;

namespace Backend.Application.Interfaces
{
    public interface ILoanService
    {
        Task<IEnumerable<LoanedBook>> GetLoanedBooksAsync();

        // Registrar la devolución de una copia.
        Task<ReturnResultDto?> RegisterReturnAsync(uint copyId);

        // Registrar un préstamo individual (método anterior).
        Task<bool> RegisterLoanAsync(uint bookId);

        // Registrar varios libros para un usuario.
        // Devuelve el resultado, el mensaje de error y el ID del préstamo.
        Task<(bool Success, string? Error, int? LoanId)> RegisterLoanAsync(
            uint borrowerId,
            IEnumerable<uint> bookIds,
            uint librarianId,
            int loanDays = 14
        );

        Task<IEnumerable<LoanListDto>> GetAllLoansAsync();

        Task<LoanDto?> GetLoanByIdAsync(uint id);

        Task<bool> UpdateLoanAsync(uint id, UpdateLoanDto dto);
    }
}