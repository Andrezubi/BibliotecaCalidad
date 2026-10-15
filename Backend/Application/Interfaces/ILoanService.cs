using Backend.Application.DTOs;
using Backend.Domain.Common;

namespace Backend.Application.Interfaces
{
    public interface ILoanService
    {
        Task<IEnumerable<LoanedBook>> GetLoanedBooksAsync();

        // Registrar la devolución de una copia. Si 'confirm' es false, solo devuelve el detalle de multa
        // sin modificar el estado en la base de datos. Si 'confirm' es true, realiza la devolución y
        // persiste la multa si corresponde.
        Task<ReturnResultDto?> RegisterReturnAsync(uint loanId, uint copyId, bool confirm = true);

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
