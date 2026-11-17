
using FrontEnd.DTOs;
using FrontEnd.Pages.Shared;
using FrontEnd.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FrontEnd.Pages.Loans
{
    public class DetailsModel : AuthorizedPageModel
    {
        private readonly LoanService _loanService;
        private readonly LoanIdProtector _idProtector;

        public DetailsModel(
            LoanService loanService,
            LoanIdProtector idProtector)
        {
            _loanService = loanService;
            _idProtector = idProtector;
        }

        // Resultado de la última devolución cuando existe multa.
        public FineReceiptDto? LastReturnResult { get; set; }

        public LoanDto? Loan { get; set; }

        public string? ErrorMessage { get; set; }

        // ID protegido recibido desde la URL.
        [BindProperty(SupportsGet = true)]
        public string Id { get; set; } = string.Empty;

        [BindProperty]
        public DateTime EditDueDate { get; set; }

        [BindProperty]
        public string EditStatus { get; set; } = string.Empty;

        // =====================================================
        // GET - MOSTRAR DETALLE DEL PRÉSTAMO
        // =====================================================

        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsInAnyRole("Admin", "Librarian"))
            {
                return RedirectToPage("/AccessDenied");
            }

            // Desproteger el ID recibido desde la URL.
            if (!_idProtector.TryUnprotect(Id, out int loanId))
            {
                return NotFound();
            }

            try
            {
                Loan = await _loanService.GetLoanByIdAsync(loanId);

                if (Loan == null)
                {
                    return NotFound();
                }

                // Cargar los valores iniciales del modal.
                EditDueDate = Loan.DueDate;
                EditStatus = Loan.Status;

                return Page();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"Error al cargar préstamo: {ex.Message}");

                ErrorMessage = "No se pudo cargar el préstamo.";

                return Page();
            }
        }

        // =====================================================
        // POST - DEVOLUCIÓN DE EJEMPLAR
        // =====================================================

        // confirm=false: previsualiza la multa sin guardar.
        // confirm=true: registra la devolución y la multa,
        // cuando corresponde.
        public async Task<IActionResult> OnPostReturnItemAsync(
            [FromForm] int copyId,
            [FromQuery] bool confirm = false)
        {
            if (!IsInAnyRole("Admin", "Librarian"))
            {
                return new JsonResult(new
                {
                    message = "No tienes permisos para realizar esta acción."
                })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
            }

            // Validar el identificador protegido del préstamo.
            if (!_idProtector.TryUnprotect(Id, out int loanId))
            {
                return NotFound(new
                {
                    message = "El identificador del préstamo no es válido."
                });
            }

            // Validar el identificador del ejemplar.
            if (copyId <= 0)
            {
                return BadRequest(new
                {
                    message = "El identificador del ejemplar no es válido."
                });
            }

            try
            {
                // Comprobar que el ejemplar pertenezca al préstamo.
                var loan = await _loanService.GetLoanByIdAsync(loanId);

                if (loan == null)
                {
                    return NotFound(new
                    {
                        message = "No se encontró el préstamo."
                    });
                }

                bool belongsToLoan = loan.Items != null &&
                    loan.Items.Any(item => item.CopyId == copyId);

                if (!belongsToLoan)
                {
                    return BadRequest(new
                    {
                        message = "El ejemplar no pertenece a este préstamo."
                    });
                }

                // Registrar o previsualizar la devolución.
                var result =
                    await _loanService.RegisterReturnWithFineAsync(
                        (uint)copyId,
                        confirm
                    );

                if (result == null)
                {
                    return BadRequest(new
                    {
                        message = "No se pudo procesar la devolución del ejemplar."
                    });
                }

                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"OnPostReturnItemAsync error: {ex.Message}");

                return StatusCode(500, new
                {
                    message = "Ocurrió un error al procesar la devolución."
                });
            }
        }

        // =====================================================
        // POST - CONFIRMAR DEVOLUCIÓN
        // =====================================================

        // Mantiene compatibilidad con el handler anterior.
        public async Task<IActionResult> OnPostConfirmReturnItemAsync(
            [FromForm] int copyId)
        {
            return await OnPostReturnItemAsync(copyId, true);
        }

        // =====================================================
        // POST - ACTUALIZAR PRÉSTAMO
        // =====================================================

        public async Task<IActionResult> OnPostUpdateLoanAsync()
        {
            if (!IsInAnyRole("Admin", "Librarian"))
            {
                return RedirectToPage("/AccessDenied");
            }

            // Recuperar el ID original antes de actualizar.
            if (!_idProtector.TryUnprotect(Id, out int loanId))
            {
                return NotFound();
            }

            try
            {
                var dto = new UpdateLoanDto
                {
                    DueDate = EditDueDate,
                    Status = EditStatus
                };

                var success =
                    await _loanService.UpdateLoanAsync(loanId, dto);

                if (success)
                {
                    TempData["Success"] =
                        "Préstamo actualizado correctamente.";
                }
                else
                {
                    TempData["Error"] =
                        "No se pudo actualizar el préstamo.";
                }

                // Mantener el identificador protegido en la URL.
                return RedirectToPage(new
                {
                    id = Id
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"OnPostUpdateLoanAsync error: {ex.Message}");

                TempData["Error"] =
                    "Ocurrió un error al actualizar el préstamo.";

                return RedirectToPage(new
                {
                    id = Id
                });
            }
        }
    }
}
