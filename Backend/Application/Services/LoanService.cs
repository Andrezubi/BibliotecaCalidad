using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Domain.Common;
using Backend.Domain.Interfaces;
using Backend.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace Backend.Application.Services
{
    public class LoanService : ILoanService
    {
        private readonly ILoanRepository _loanRepository;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public LoanService(ILoanRepository loanRepository, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _loanRepository = loanRepository;
            _configuration = configuration;
        }

        public async Task<IEnumerable<LoanedBook>> GetLoanedBooksAsync()
        {
            return await _loanRepository.GetLoanedBooksAsync();
        }

        // If confirm == false, do not persist changes; only compute fine details so UI can show modal.
        public async Task<ReturnResultDto?> RegisterReturnAsync(uint copyId, bool confirm = false)
        {
            var copy = await _loanRepository.GetCopyByIdAsync(copyId);

            // La copia debe existir y estar prestada.
            if (copy == null)
                return null;

            if (copy.StatusId != 2) // Loaned
                return new ReturnResultDto { HasFine = false, Message = "La copia no está prestada." };

            // 2) Obtener el préstamo activo que contiene esta copia
            var loan = await _loanRepository.GetActiveLoanByCopyIdAsync(copyId);

            if (loan == null)
                return new ReturnResultDto { HasFine = false, Message = "No se encontró un préstamo activo para esta copia." };

            // Permitimos devolver un ítem que esté Active u Overdue
            var loanItem = loan.Loanitems.FirstOrDefault(li => li.CopyId == copyId && (li.Status == "Active" || li.Status == "Overdue"));

            if (loanItem == null)
                return new ReturnResultDto { HasFine = false, Message = "El ejemplar no está pendiente de devolución." };

            // Verificar si ya fue devuelto (doble devolución)
            if (loanItem.Status == "Returned")
                return new ReturnResultDto { HasFine = false, Message = "Este ejemplar ya fue devuelto." };

            // Calcular días de retraso basado en la fecha de vencimiento del préstamo vs fecha actual
            var returnDate = DateTime.Now;
            var overdueDays = (returnDate.Date - loan.DueDate.Date).Days;
            if (overdueDays < 0) overdueDays = 0;

            // Leer tarifa diaria desde configuración
            decimal dailyRate = 10.00m;
            try
            {
                var cfg = _configuration["Fine:DailyRate"];
                if (!string.IsNullOrWhiteSpace(cfg))
                {
                    if (decimal.TryParse(cfg, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                    {
                        dailyRate = parsed;
                    }
                }
            }
            catch
            {
                // Ignorar errores y usar valor por defecto
            }

            var amount = dailyRate * overdueDays;
            var hasFine = overdueDays > 0;

            // Verificar si ya existe multa para este loan+copy
            var existingFine = await _loanRepository.GetFineByLoanCopyAsync(loan.Id, copyId);

            if (!confirm)
            {
                // Preview mode: solo calcular y devolver información sin persistir
                if (hasFine)
                {
                    return new ReturnResultDto
                    {
                        HasFine = true,
                        Message = existingFine != null ? "Multa existente." : "Próxima devolución generará una multa.",
                        CopyId = (int)copyId,
                        Amount = existingFine?.Amount ?? amount,
                        OverdueDays = existingFine != null ? (int)existingFine.OverdueDays : overdueDays,
                        BorrowerName = loan.Borrower.FirstName + " " + loan.Borrower.LastName,
                        BookTitle = loanItem.Copy.Book.Title
                    };
                }
                return new ReturnResultDto { HasFine = false, Message = "Devolución sin multa." };
            }

            // Confirm mode: persistir todos los cambios en una transacción
            using var tx = await _loanRepository.BeginTransactionAsync();
            try
            {
                // 1) Actualizar copia a Disponible
                copy.StatusId = 1; // Available
                copy.UpdatedAt = returnDate;

                // 2) Actualizar LoanItem
                loanItem.ReturnedAt = returnDate;
                loanItem.Status = "Returned";
                loanItem.UpdatedAt = returnDate;

                // 3) Actualizar estado general del préstamo
                if (loan.Loanitems.All(li => li.Status == "Returned"))
                {
                    loan.Status = "Completed";
                }
                else if (loan.DueDate.Date < returnDate.Date && loan.Loanitems.Any(li => li.Status == "Active" || li.Status == "Overdue"))
                {
                    loan.Status = "Overdue";
                }
                else
                {
                    loan.Status = "Active";
                }
                loan.UpdatedAt = returnDate;

                // 4) Crear multa si corresponde y no existe
                if (hasFine && existingFine == null)
                {
                    var fine = new Fine
                    {
                        LoanId = loan.Id,
                        CopyId = copyId,
                        DailyRate = dailyRate,
                        OverdueDays = (uint)overdueDays,
                        Amount = amount,
                        Status = "Pending",
                        CreatedAt = returnDate,
                        UpdatedAt = returnDate
                    };

                    Backend.Domain.Validators.FineValidator.Validate(fine);
                    await _loanRepository.AddFineAsync(fine);
                }

                // 5) Guardar todos los cambios
                await _loanRepository.SaveChangesAsync();
                await tx.CommitAsync();

                return new ReturnResultDto
                {
                    HasFine = hasFine,
                    Message = hasFine ? "Devolución registrada. Multa generada." : "Devolución registrada correctamente.",
                    CopyId = (int)copyId,
                    Amount = hasFine ? (existingFine?.Amount ?? amount) : 0,
                    OverdueDays = hasFine ? (existingFine != null ? (int)existingFine.OverdueDays : overdueDays) : 0,
                    BorrowerName = loan.Borrower.FirstName + " " + loan.Borrower.LastName,
                    BookTitle = loanItem.Copy.Book.Title
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<ReturnResultDto?> RegisterReturnAsync(uint loanId, uint copyId, bool confirm = true)
        {
            var loan = await _loanRepository.GetLoanWithItemsByIdAsync(loanId);
            var item = loan?.Loanitems.FirstOrDefault(li => li.CopyId == copyId);
            if (loan == null || item == null) return null;
            if (item.Status != "Active" && item.Status != "Overdue")
                return new ReturnResultDto { Success = false, Message = "El ejemplar no admite devolución." };

            var now = DateTime.Now;
            var daysLate = Math.Max(0, (now.Date - loan.DueDate.Date).Days);
            var fine = daysLate > 0 ? await _loanRepository.GetFineByLoanCopyAsync(loanId, copyId) : null;
            var dailyRate = GetDailyFineRate();
            if (!confirm)
            {
                var previewRate = fine?.DailyRate ?? dailyRate;
                return new ReturnResultDto { HasFine = daysLate > 0, Message = daysLate > 0 ? "Se generará una multa al confirmar la devolución." : "El ejemplar está dentro del plazo; no se generará multa.",
                    CopyId = (int)copyId, Amount = fine?.Amount ?? previewRate * daysLate,
                    OverdueDays = fine == null ? daysLate : (int)fine.OverdueDays,
                    BorrowerName = $"{loan.Borrower.FirstName} {loan.Borrower.LastName}".Trim(),
                    BookTitle = item.Copy.Book.Title, FineStatus = fine?.Status ?? (daysLate > 0 ? "Pending" : null),
                    DailyRate = daysLate > 0 ? previewRate : null, DueDate = loan.DueDate, LoanId = (int)loan.Id };
            }

            await using var transaction = await _loanRepository.BeginTransactionAsync();
            if (daysLate > 0 && fine == null)
            {
                fine = new Fine { LoanId = loanId, CopyId = copyId, DailyRate = dailyRate,
                    OverdueDays = (uint)daysLate, Amount = dailyRate * daysLate,
                    Status = "Pending", CreatedAt = now, UpdatedAt = now };
                Backend.Domain.Validators.FineValidator.Validate(fine);
                await _loanRepository.AddFineAsync(fine);
            }

            item.ReturnedAt = now;
            item.Status = "Returned";
            item.UpdatedAt = now;
            item.Copy.StatusId = 1;
            item.Copy.UpdatedAt = now;
            var remaining = loan.Loanitems.Where(li => li.CopyId != copyId && li.Status != "Returned" && li.Status != "Lost").ToList();
            loan.Status = remaining.Count == 0 ? "Completed" : loan.DueDate.Date < now.Date ? "Overdue" : "Active";
            loan.UpdatedAt = now;
            await _loanRepository.SaveChangesAsync();
            await transaction.CommitAsync();

            return new ReturnResultDto { HasFine = fine != null,
                Message = fine == null ? "Devolución registrada correctamente." : "Devolución registrada. Multa generada.",
                CopyId = (int)copyId, Amount = fine?.Amount ?? 0, OverdueDays = fine == null ? 0 : (int)fine.OverdueDays,
                BorrowerName = $"{loan.Borrower.FirstName} {loan.Borrower.LastName}".Trim(),
                BookTitle = item.Copy.Book.Title, PaidAt = now, FineStatus = fine?.Status,
                DailyRate = fine?.DailyRate, DueDate = loan.DueDate, LoanId = (int)loan.Id };
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

        public Task<bool> RegisterLoanAsync(uint bookId)
        {
            // Método antiguo no implementado; use RegisterLoanAsync con parámetros completos.
            return Task.FromResult(false);
        }

        public async Task<bool> UpdateLoanAsync(uint id, UpdateLoanDto dto)
        {
            // Intentar obtener el préstamo
            var loan = await _loanRepository.GetLoanWithItemsByIdAsync(id);
            if (loan == null)
                return false;

            // Actualizar campos permitidos
            loan.DueDate = dto.DueDate;
            loan.Status = dto.Status;
            loan.UpdatedAt = DateTime.Now;

            // Ajustar el estado de los ítems según la nueva fecha de vencimiento o el estado indicado
            // Si la nueva fecha ya pasó o el estado se marca como Overdue, marcar ítems activos como Overdue.
            // Si el estado se marca como Active y la nueva fecha no ha pasado, restaurar ítems Overdue a Active.
            var now = DateTime.Now;
            var duePassed = loan.DueDate.Date < now.Date;

            if (dto.Status == "Overdue" || duePassed)
            {
                foreach (var li in loan.Loanitems.Where(x => x.Status == "Active"))
                {
                    li.Status = "Overdue";
                    li.UpdatedAt = DateTime.Now;
                }
            }
            else if (dto.Status == "Active" && !duePassed)
            {
                foreach (var li in loan.Loanitems.Where(x => x.Status == "Overdue"))
                {
                    li.Status = "Active";
                    li.UpdatedAt = DateTime.Now;
                }
            }

            // Persistir cambios
            await _loanRepository.SaveChangesAsync();

            return true;
        }

        private static LoanDto MapToDto(Loan loan)
        {
            var overdue = loan.DueDate.Date < DateTime.Now.Date;
            var loanDto = new LoanDto
            {
                Id = (int)loan.Id,
                BorrowerId = loan.BorrowerId,
                BorrowerName = $"{loan.Borrower.FirstName} {loan.Borrower.LastName}".Trim(),
                LibrarianId = loan.LibrarianId,
                LibrarianName = $"{loan.Librarian.FirstName} {loan.Librarian.LastName}".Trim(),
                LoanDate = loan.LoanDate,
                DueDate = loan.DueDate,
                Status = loan.Status == "Completed" || loan.Status == "Lost" ? loan.Status : overdue ? "Overdue" : "Active",
                StatusDisplayName = GetLoanStatusDisplayName(loan.Status == "Completed" || loan.Status == "Lost" ? loan.Status : overdue ? "Overdue" : "Active"),
                CreatedAt = loan.CreatedAt,
                UpdatedAt = loan.UpdatedAt,
                Items = loan.Loanitems.Select(li => new LoanItemDto
                {
                    CopyId = (int)li.CopyId,
                    InternalCode = li.Copy.InternalCode,
                    BookId = (int)li.Copy.BookId,
                    BookTitle = li.Copy.Book.Title,
                    ReturnedAt = li.ReturnedAt,
                    Status = li.Status == "Returned" || li.Status == "Lost" ? li.Status : overdue ? "Overdue" : "Active",
                    StatusDisplayName = GetLoanItemStatusDisplayName(li.Status == "Returned" || li.Status == "Lost" ? li.Status : overdue ? "Overdue" : "Active")
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

        private decimal GetDailyFineRate()
        {
            var configuredRate = _configuration["Fine:DailyRate"];
            if (decimal.TryParse(configuredRate, System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var rate) && rate > 0)
                return rate;

            return 10.00m; // Fine.DailyRate default defined by the current database model.
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
