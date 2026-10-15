using System.Linq;
using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoansController : ControllerBase
    {
        private readonly ILoanService _loanService;

        public LoansController(ILoanService loanService)
        {
            _loanService = loanService;
        }

        [HttpGet("loaned-books")]
        public async Task<IActionResult> GetLoanedBooks()
        {
            var books = await _loanService.GetLoanedBooksAsync();
            return Ok(books);
        }

        [HttpPost("return/{copyId}")]
        public async Task<IActionResult> RegisterReturn(int copyId, [FromQuery] bool confirm = true)
        {
            // Si confirm == false -> solo calcular/mostrar multa sin persistir la devolución
            var result = await _loanService.RegisterReturnAsync((uint)copyId, confirm);

            if (result == null)
            {
                return BadRequest(new
                {
                    message = "No se pudo procesar la operación. La copia no existe o no está prestada."
                });
            }

            return Ok(result);
        }

        [HttpPost("loan-book/{bookId}")]
        public async Task<IActionResult> RegisterLoan(int bookId, [FromQuery] int borrowerId)
        {
            if (borrowerId <= 0)
            {
                return BadRequest(new { message = "Debe indicar borrowerId como parámetro de consulta." });
            }

            var (success, error, loanId) = await _loanService.RegisterLoanAsync((uint)borrowerId, new[] { (uint)bookId }, 1);

            if (!success)
            {
                return BadRequest(new { message = error });
            }

            return Ok(new { message = "Préstamo registrado.", loanId });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Backend.Application.DTOs.LoanCreateRequest request)
        {
            if (request == null || request.BookIds == null || !request.BookIds.Any())
                return BadRequest(new { message = "Solicitud inválida. Se requieren BorrowerId y al menos un BookId." });

            var librarianId = request.LibrarianId ?? 1;

            var (success, error, loanId) = await _loanService.RegisterLoanAsync(request.BorrowerId, request.BookIds, librarianId, request.LoanDays);

            if (!success)
                return BadRequest(new { message = error });

            return Ok(new { message = "Préstamo creado", loanId });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var loans = await _loanService.GetAllLoansAsync();
            return Ok(loans);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var loan = await _loanService.GetLoanByIdAsync((uint)id);

            if (loan == null)
                return NotFound();

            return Ok(loan);
        }
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateLoan(int id, [FromBody] UpdateLoanDto dto)
        {
            var updated = await _loanService.UpdateLoanAsync((uint)id, dto);
            if (!updated)
                return NotFound(new { message = "Préstamo no encontrado." });

            return Ok(new { message = "Préstamo actualizado exitosamente." });
        }
    }
}