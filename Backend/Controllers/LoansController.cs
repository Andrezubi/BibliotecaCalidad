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
        public async Task<IActionResult> RegisterReturn(int copyId)
        {
            var result = await _loanService.RegisterReturnAsync(copyId);

            if (!result)
            {
                return BadRequest(new
                {
                    message = "No se pudo registrar la devolución. " +
                              "La copia no existe o no está prestada."
                });
            }

            return Ok(new
            {
                message = "Devolución registrada. El libro ahora está disponible."
            });
        }
        [HttpPost("loan-book/{bookId}")]
        public async Task<IActionResult> RegisterLoan(int bookId)
        {
            var result = await _loanService.RegisterLoanAsync(bookId);

            if (!result)
            {
                return BadRequest(new
                {
                    message = "No se pudo registrar el préstamo. " +
                              "El libro no tiene copias disponibles."
                });
            }

            return Ok(new
            {
                message = "Préstamo registrado. El libro ahora está prestado."
            });
        }
    }

}