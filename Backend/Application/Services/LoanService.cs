using Backend.Application.DTOs;
using Backend.Domain.Common;

namespace Backend.Application.Interfaces
{
    public interface ILoanService
    {
        Task<IEnumerable<LoanedBook>> GetLoanedBooksAsync();

        // Registra la devolución de una copia.
        Task<ReturnResultDto?> RegisterReturnAsync(uint copyId);

        // Método anterior: conserva el registro de un préstamo individual.
        Task<bool> RegisterLoanAsync(uint bookId);

        // Nuevo método: registra un préstamo de varios libros
        // para un usuario, indicando el bibliotecario responsable.
        // Devuelve: éxito, mensaje de error e identificador del préstamo.
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