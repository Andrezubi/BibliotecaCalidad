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

            // 1) Cambiar el estado de la copia a Disponible solo si confirmamos la devolución.
            if (confirm)
            {
                copy.StatusId = 1; // Available
                copy.UpdatedAt = DateTime.Now;
            }

            // 2) Registrar la devolución en el préstamo activo, si existe.
            var loan = await _loanRepository.GetActiveLoanByCopyIdAsync(copyId);

            if (loan != null)
            {
                // Permitimos devolver un ítem que esté Active u Overdue
                var loanItem = loan.Loanitems.FirstOrDefault(li => li.CopyId == copyId && (li.Status == "Active" || li.Status == "Overdue"));
                string? prevStatus = null;
                if (loanItem != null)
                {
                    // Guardar estado previo antes de modificar para el cálculo de multa y lógica UI
                    prevStatus = loanItem.Status;

                    if (confirm)
                    {
                        loanItem.ReturnedAt = DateTime.Now;
                        loanItem.Status = "Returned";
                        loanItem.UpdatedAt = DateTime.Now;
                    }
                }

                // Update loan status only if confirm == true (we persist changes)
                if (confirm)
                {
                    if (loan.Loanitems.All(li => li.Status == "Returned"))
                    {
                        loan.Status = "Completed";
                        loan.UpdatedAt = DateTime.Now;
                    }
                    else
                    {
                        var now = DateTime.Now;
                        // consider both Active and Overdue as non-returned
                        if (loan.DueDate.Date < now.Date && loan.Loanitems.Any(li => li.Status == "Active" || li.Status == "Overdue"))
                        {
                            loan.Status = "Overdue";
                        }
                        else
                        {
                            loan.Status = "Active";
                        }

                        loan.UpdatedAt = DateTime.Now;
                    }
                }

                // 3) Calcular multa si la devolución está fuera de la fecha límite
                // Determinamos los días de retraso a partir de la fecha de vencimiento del préstamo.
                var overdueDays = 0;
                // Si el ítem estaba marcado como Overdue o la fecha actual supera la fecha de vencimiento,
                // consideramos que existe retraso. Si el ítem estaba manualmente marcado como Overdue pero
                // la DueDate no refleja un día pasado, usamos al menos 1 día de retraso para forzar la creación
                // de la multa y mostrar el recibo (comportamiento requerido por la UI).
                if (loanItem != null)
                {
                    if (prevStatus == "Overdue")
                    {
                        overdueDays = (DateTime.Now.Date - loan.DueDate.Date).Days;
                        if (overdueDays <= 0) overdueDays = 1;
                    }
                    else
                    {
                        overdueDays = (DateTime.Now.Date - loan.DueDate.Date).Days;
                    }
                }

                if (overdueDays > 0 && loanItem != null)
                {
                    // Evitar multas duplicadas comprobando si ya existe una multa para este loan+copy
                    var existingFine = await _loanRepository.GetFineByLoanCopyAsync(loan.Id, copyId);

                    // Intentar leer la tarifa diaria desde configuración (appsettings: "Fine:DailyRate").
                    // Si no está configurada, usar un valor por defecto documentado.
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

                    if (existingFine == null)
                    {
                        // Si no confirmamos, devolvemos solo el detalle de la multa (preview) sin persistir
                        if (!confirm)
                        {
                            return new ReturnResultDto
                            {
                                HasFine = true,
                                Message = "Próxima devolución generará una multa.",
                                CopyId = (int)copyId,
                                Amount = amount,
                                OverdueDays = overdueDays,
                                BorrowerName = loan.Borrower.FirstName + " " + loan.Borrower.LastName,
                                BookTitle = loanItem.Copy.Book.Title
                            };
                        }

                        // confirm == true -> persistir la multa
                        var fine = new Fine
                        {
                            LoanId = loan.Id,
                            CopyId = copyId,
                            DailyRate = dailyRate,
                            OverdueDays = (uint)overdueDays,
                            Amount = amount,
                            Status = "Pending",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };

                        // Validar la entidad antes de persistir
                        Backend.Domain.Validators.FineValidator.Validate(fine);

                        await _loanRepository.AddFineAsync(fine);

                        // Guardar la multa y demás cambios
                        await _loanRepository.SaveChangesAsync();

                        return new ReturnResultDto
                        {
                            HasFine = true,
                            Message = "Devolución registrada. Multa generada.",
                            CopyId = (int)copyId,
                            Amount = fine.Amount,
                            OverdueDays = (int)fine.OverdueDays,
                            BorrowerName = loan.Borrower.FirstName + " " + loan.Borrower.LastName,
                            BookTitle = loanItem.Copy.Book.Title
                        };
                    }
                    else
                    {
                        // Si ya existe multa, reportarla. No necesitamos crearla de nuevo.
                        return new ReturnResultDto
                        {
                            HasFine = true,
                            Message = "Multa existente.",
                            CopyId = (int)copyId,
                            Amount = existingFine.Amount,
                            OverdueDays = (int)existingFine.OverdueDays,
                            BorrowerName = loan.Borrower.FirstName + " " + loan.Borrower.LastName,
                            BookTitle = loanItem.Copy.Book.Title
                        };
                    }
                }
            }

            await _loanRepository.SaveChangesAsync();

            return new ReturnResultDto { HasFine = false, Message = "Devolución registrada correctamente." };
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