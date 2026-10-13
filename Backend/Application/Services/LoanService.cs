using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Domain.Common;
using Backend.Domain.Interfaces;
using Backend.Models;

namespace Backend.Application.Services
{
    public class LoanService : ILoanService
    {
        private readonly ILoanRepository _loanRepository;

        public LoanService(ILoanRepository loanRepository)
        {
            _loanRepository = loanRepository;
        }

        public async Task<IEnumerable<LoanedBook>> GetLoanedBooksAsync()
        {
            return await _loanRepository.GetLoanedBooksAsync();
        }

        public async Task<bool> RegisterReturnAsync(uint copyId)
        {
            var copy = await _loanRepository.GetCopyByIdAsync(copyId);

            // La copia debe existir y estar prestada.
            if (copy == null)
                return false;

            if (copy.StatusId != 2) // Loaned
                return false;

            // 1) Cambiar el estado de la copia a Disponible.
            copy.StatusId = 1; // Available
            copy.UpdatedAt = DateTime.Now;

            // 2) Registrar la devolución en el préstamo activo, si existe.
            var loan = await _loanRepository.GetActiveLoanByCopyIdAsync(copyId);

            if (loan != null)
            {
                var loanItem = loan.Loanitems.FirstOrDefault(li => li.CopyId == copyId && li.Status == "Active");
                if (loanItem != null)
                {
                    loanItem.ReturnedAt = DateTime.Now;
                    loanItem.Status = "Returned";
                    loanItem.UpdatedAt = DateTime.Now;
                }
                
                // Check if all items are returned
                if (loan.Loanitems.All(li => li.Status == "Returned"))
                {
                    loan.Status = "Completed";
                    loan.UpdatedAt = DateTime.Now;
                }
            }

            await _loanRepository.SaveChangesAsync();

            return true;
        }

        public async Task<(bool Success, string? Error, int? LoanId)> RegisterLoanAsync(uint borrowerId, IEnumerable<uint> bookIds, uint librarianId, int loanDays = 14)
        {
            if (borrowerId == 0)

            // borrowerId and librarianId are uint in the service signature; cast to model types when creating the Loan entity.
                return (false, "BorrowerId inválido.", null);

            var bookIdList = bookIds?.ToList() ?? new List<uint>();
            if (!bookIdList.Any())
                return (false, "No se han especificado libros para el préstamo.", null);

            // Iniciar transacción
            var tx = await _loanRepository.BeginTransactionAsync();
            try
            {
                var reservedCopies = new List<Copy>();
                var unavailable = new List<uint>();

                // Intentar reservar una copia por cada bookId usando FOR UPDATE en repositorio
                foreach (var bookId in bookIdList)
                {
                    var copy = await _loanRepository.GetFirstAvailableCopyByBookIdForUpdateAsync(bookId);

                    // DEBUG: comprobar si la consulta FOR UPDATE devuelve resultado
                    Console.WriteLine($"[LoanService] Buscar copia FOR UPDATE para BookId={bookId} -> {(copy != null ? "Found Id=" + copy.Id.ToString() : "null")}");

                    if (copy == null)
                    {
                        // Intento alternativo sin FOR UPDATE para ver si hay copias disponibles
                        var copyNoLock = await _loanRepository.GetFirstAvailableCopyByBookIdAsync(bookId);
                        Console.WriteLine($"[LoanService] Buscar copia sin lock para BookId={bookId} -> {(copyNoLock != null ? "Found Id=" + copyNoLock.Id.ToString() : "null")}");

                        unavailable.Add(bookId);
                        continue;
                    }

                    copy.StatusId = 2; // Loaned
                    copy.UpdatedAt = DateTime.Now;
                    reservedCopies.Add(copy);
                }

                if (unavailable.Any())
                {
                    await tx.RollbackAsync();
                    return (false, $"No hay copias disponibles para: {string.Join(',', unavailable)}", null);
                }

                // Persistir los cambios de estado de las copias reservadas dentro de la transacción
                // (marcamos las copias como prestadas antes de crear el registro de Loan)
                await _loanRepository.SaveChangesAsync();

                // Crear entidad Loan con Loanitems
                var loan = new Loan
                {
                    BorrowerId = (ushort)borrowerId,
                    LibrarianId = (ushort)librarianId,
                    LoanDate = DateTime.Now,
                    DueDate = DateTime.Now.AddDays(loanDays),
                    Status = "Active",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Loanitems = reservedCopies.Select(c => new Loanitem
                    {
                        CopyId = c.Id,
                        Status = "Active",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    }).ToList()
                };

                await _loanRepository.AddLoanAsync(loan);

                // Guardar el Loan y sus items antes de confirmar la transacción
                await _loanRepository.SaveChangesAsync();

                await tx.CommitAsync();

                return (true, null, (int?)loan.Id);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, ex.Message, null);
            }
        }

        public async Task<IEnumerable<LoanListDto>> GetAllLoansAsync()
        {
            return await _loanRepository.GetAllLoansAsync();
        }

        public async Task<LoanDto?> GetLoanByIdAsync(uint id)
        {
            var loan = await _loanRepository.GetLoanWithItemsByIdAsync(id);

            if (loan == null)
                return null;

            return MapToDto(loan);
        }

        private static LoanDto MapToDto(Loan loan)
        {
            var loanDto = new LoanDto
            {
                Id = (int)loan.Id,
                BorrowerId = loan.BorrowerId,
                BorrowerName = $"{loan.Borrower.FirstName} {loan.Borrower.LastName}".Trim(),
                LibrarianId = loan.LibrarianId,
                LibrarianName = $"{loan.Librarian.FirstName} {loan.Librarian.LastName}".Trim(),
                LoanDate = loan.LoanDate,
                DueDate = loan.DueDate,
                Status = loan.Status,
                StatusDisplayName = GetLoanStatusDisplayName(loan.Status),
                CreatedAt = loan.CreatedAt,
                UpdatedAt = loan.UpdatedAt,
                Items = loan.Loanitems.Select(li => new LoanItemDto
                {
                    CopyId = (int)li.CopyId,
                    InternalCode = li.Copy.InternalCode,
                    BookId = (int)li.Copy.BookId,
                    BookTitle = li.Copy.Book.Title,
                    ReturnedAt = li.ReturnedAt,
                    Status = li.Status,
                    StatusDisplayName = GetLoanItemStatusDisplayName(li.Status)
                }).ToList()
            };

            return loanDto;
        }

        private static string GetLoanStatusDisplayName(string status)
        {
            return status switch
            {
                "Active" => "Activo",
                "Completed" => "Completado",
                "Overdue" => "Vencido",
                "Lost" => "Perdido",
                _ => status
            };
        }

        private static string GetLoanItemStatusDisplayName(string status)
        {
            return status switch
            {
                "Active" => "Activo",
                "Returned" => "Devuelto",
                "Overdue" => "Vencido",
                "Lost" => "Perdido",
                _ => status
            };
        }
    }
}